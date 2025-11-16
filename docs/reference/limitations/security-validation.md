# Security & Validation Limitations

## Overview

Folly implements several security hardening measures including XXE prevention, resource limits, and path validation. However, some validation and security features are not yet complete.

## Current Security Implementation

**Location**: Various files

### ✅ Implemented Security Features

#### 1. XXE (XML External Entity) Prevention

**Location**: `src/Folly.Core/Dom/FoParser.cs`

**Protection**:
```csharp
var settings = new XmlReaderSettings
{
    DtdProcessing = DtdProcessing.Prohibit,  // Disable DTD
    XmlResolver = null,  // Disable external entity resolution
    MaxCharactersFromEntities = 0
};
```

**Test**: `SecurityTests.Load_RejectsXXE_EntityExpansionBomb` (passing)

**Protected Against**:
- Billion laughs attack (entity expansion bomb)
- External entity file inclusion
- DTD-based attacks

#### 2. Resource Limits

**Location**: `src/Folly.Core/LayoutOptions.cs`

**Limits**:
```csharp
public class LayoutOptions
{
    public int MaxPages = 10000;              // Prevent infinite pagination
    public long MaxImageSizeBytes = 50MB;     // Prevent OOM from huge images
    public int MaxTableCells = 100000;        // Prevent computational DoS
    public int MaxNestingDepth = 100;         // Prevent stack overflow
}
```

**Purpose**: Prevent denial-of-service attacks

#### 3. Image Path Validation

**Location**: `src/Folly.Core/Layout/LayoutEngine.cs:1604-1646`

**Validation**:
```csharp
private bool ValidateImagePath(string imagePath)
{
    // Prevent path traversal (../)
    var fullPath = Path.GetFullPath(imagePath);

    // Check absolute path policy
    if (Path.IsPathRooted(imagePath) && !_options.AllowAbsoluteImagePaths)
        return false;

    // Verify within allowed base path
    if (!string.IsNullOrWhiteSpace(_options.AllowedImageBasePath))
    {
        // Ensure path starts with base path
    }
}
```

**Protected Against**:
- Path traversal attacks (`../../etc/passwd`)
- Arbitrary file access
- Directory escape

**Test**: `SecurityTests.LayoutImage_RejectsPathTraversal_WhenBasePathSet` (passing)

#### 4. PDF Metadata Sanitization

**Location**: `src/Folly.Pdf/PdfRenderer.cs`

**Sanitization**:
```csharp
// Remove null bytes
metadata = metadata.Replace("\0", "");

// Escape special PDF characters
metadata = metadata.Replace("\\", "\\\\")
                   .Replace("(", "\\(")
                   .Replace(")", "\\)")
                   .Replace("\r", "\\r")
                   .Replace("\n", "\\n");
```

**Test**: `SecurityTests.PdfMetadata_RemovesNullBytes`, `SecurityTests.PdfMetadata_EscapesSpecialCharacters` (passing)

**Protected Against**:
- PDF injection via metadata
- Malformed PDF strings
- Null byte attacks

## Limitations / Not Implemented

### 1. Namespace Validation Not Enforced

**Severity**: Medium
**Status**: Explicitly disabled

**Test**:
```csharp
[Fact(Skip = "Namespace validation not yet enforced by parser")]
public void MissingNamespace_HandlesGracefully()
{
    // Should reject FO without proper namespace
}
```

**Current Behavior**: Accepts XSL-FO XML without namespace declaration

**Example Accepted** (but shouldn't be):
```xml
<root>  <!-- Missing xmlns="http://www.w3.org/1999/XSL/Format" -->
  <page-sequence master-reference="A4">
    ...
  </page-sequence>
</root>
```

**Correct**:
```xml
<fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
  ...
</fo:root>
```

**Impact**:
- May process invalid FO documents
- Could lead to unexpected behavior
- Violates XSL-FO spec

**Proposed Fix**:
```csharp
if (reader.NamespaceURI != "http://www.w3.org/1999/XSL/Format")
{
    throw new InvalidDataException(
        "Root element must be in XSL-FO namespace");
}
```

### 2. No Schema Validation

**Severity**: Low to Medium
**Missing**: XSD schema validation

**Description**:
- No validation against XSL-FO 1.1 schema
- Invalid FO structure may be accepted
- Errors only caught during layout

**Example Accepted** (but invalid):
```xml
<fo:block>
  <fo:table>  <!-- fo:table not allowed as direct child of fo:block -->
    ...
  </fo:table>
</fo:block>
```

**Correct**:
```xml
<fo:block>
  Text content only, or fo:inline, fo:external-graphic, etc.
</fo:block>
<!-- Table should be sibling, not child -->
<fo:table>...</fo:table>
```

**Current**: Parser accepts structure, layout may fail or produce incorrect output

**Proposed**: Validate against XSL-FO XSD schema during parsing

**Trade-off**:
- Pros: Catch errors early, better error messages
- Cons: Performance overhead, stricter parsing

### 3. No Input Size Limit

**Severity**: Low
**Missing**: Maximum document size limit

**Current**: Can load arbitrarily large FO XML files

**Risk**: Memory exhaustion

**Proposed**:
```csharp
public class LayoutOptions
{
    public long MaxDocumentSizeBytes = 100 * 1024 * 1024;  // 100 MB
}
```

**Mitigation**: Limit is mostly handled by OS/runtime memory limits

### 4. No URL Scheme Validation

**Severity**: Low
**Elements**: `fo:external-graphic`, `fo:basic-link`

**Description**:
- No whitelist of allowed URL schemes
- Could reference `file://`, `javascript:`, etc.

**Example Risk**:
```xml
<fo:basic-link external-destination="javascript:alert('XSS')">
  Click me
</fo:basic-link>
```

**In PDF**: JavaScript can be embedded in PDF actions

**Current**: Accepts any URL scheme

**Proposed Whitelist**:
- `http://`
- `https://`
- `mailto:`
- `file://` (if explicitly allowed)
- Block: `javascript:`, `data:`, `vbscript:`, etc.

### 5. No Rate Limiting

**Severity**: Low
**Context**: Server/service deployment

**Description**:
- No built-in protection against repeated requests
- Could be used for resource exhaustion

**Impact**: DoS if deployed as service

**Mitigation**: Should be handled at application/server level, not library level

### 6. No Content Security Policy

**Severity**: Very Low
**PDF Feature**: CSP-like restrictions

**Description**:
- No way to restrict what FO document can do
- Cannot disable JavaScript in generated PDF
- Cannot restrict embedded resources

**Example Policy** (not supported):
```csharp
var options = new LayoutOptions
{
    AllowJavaScript = false,  // Not implemented
    AllowExternalResources = false,  // Partially via AllowedImageBasePath
    AllowEmbeddedFiles = false,  // Not implemented
};
```

### 7. Limited Property Validation

**Severity**: Low
**Current**: Basic type validation (colors, lengths)

**Not Validated**:
- Property value ranges (e.g., font-size > 0)
- Property combinations (conflicting properties)
- Circular references (in property inheritance)

**Example Accepted** (but invalid):
```xml
<fo:block font-size="-10pt">  <!-- Negative font size! -->
  Invalid
</fo:block>
```

**Current**: May cause layout errors or undefined behavior

**Proposed**: Validate property values against spec constraints

### 8. No Sandboxing

**Severity**: Low for current implementation
**Future**: If XSLT or scripting support added

**Description**:
- No sandbox environment for processing
- If XSLT preprocessing added, would need sandboxing
- If JavaScript in PDF enabled, would need restrictions

**Current**: Not applicable (no scripting)

**Future Consideration**: If adding XSLT transformation:
- Disable document() function
- Disable extension functions
- Limit recursion depth
- Timeout transformations

## Security Best Practices

### For Library Users

1. **Set Resource Limits**:
```csharp
var options = new LayoutOptions
{
    MaxPages = 1000,  // Your limit
    MaxImageSizeBytes = 10 * 1024 * 1024,  // 10 MB
    MaxTableCells = 50000,
    AllowedImageBasePath = "/safe/images/directory",
    AllowAbsoluteImagePaths = false
};
```

2. **Validate Input Source**:
```csharp
// Only process FO from trusted sources
// Validate XML before passing to Folly
```

3. **Handle Exceptions**:
```csharp
try
{
    var doc = FoDocument.Load(stream);
}
catch (InvalidOperationException ex)
{
    // Handle max pages exceeded
}
catch (InvalidDataException ex)
{
    // Handle malformed FO
}
```

4. **Consider Timeouts**:
```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
// Layout operations could be cancellable in future versions
```

### For Deployment

1. **Isolate Process**: Run in separate process/container
2. **Memory Limits**: Set process memory limits
3. **CPU Limits**: Set CPU quotas
4. **Disk Quotas**: Limit temporary file creation
5. **Network Isolation**: Block network access if not needed

## XSL-FO Validation Resources

**XSD Schema**:
- Official XSL-FO 1.1 schema
- Can be used for pre-validation
- https://www.w3.org/TR/xsl11/#fo-sec-schema

**RELAX NG Schema**:
- Alternative schema language
- More expressive than XSD
- Available for XSL-FO

## Testing

**Current Security Tests** (in `SecurityTests.cs`):
1. ✅ XXE rejection
2. ✅ Entity expansion bomb rejection
3. ✅ Path traversal prevention
4. ✅ Absolute path restriction
5. ✅ Null byte removal from metadata
6. ✅ Special character escaping in metadata

**Missing Tests**:
1. ❌ Namespace validation
2. ❌ Schema validation
3. ❌ Document size limits
4. ❌ URL scheme validation
5. ❌ Property value validation
6. ❌ Nesting depth limits (tested via fuzzing but no explicit test)

## Proposed Implementation Priorities

### High Priority
1. **Enforce namespace validation** - Easy fix, important for correctness
2. **Add document size limit** - Prevent OOM

### Medium Priority
3. **URL scheme whitelist** - Security hardening
4. **Property value validation** - Correctness and error messages

### Low Priority
5. **Schema validation** - Nice to have, performance cost
6. **Content security policy** - Advanced feature

## References

1. **OWASP XML Security Cheat Sheet**:
   - https://cheatsheetseries.owasp.org/cheatsheets/XML_Security_Cheat_Sheet.html

2. **XXE Prevention**:
   - https://owasp.org/www-community/vulnerabilities/XML_External_Entity_(XXE)_Processing

3. **PDF Security**:
   - PDF Reference 1.7, Section 3.5: Encryption
   - https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/PDF32000_2008.pdf

## See Also

- [performance.md](performance.md) - Resource limits and DoS prevention
- [images.md](images.md) - Image path validation details
