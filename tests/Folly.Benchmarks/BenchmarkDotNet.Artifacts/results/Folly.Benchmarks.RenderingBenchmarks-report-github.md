```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.3 LTS (Noble Numbat)
unknown, 1 CPU, 16 logical and 16 physical cores
.NET SDK 8.0.121
  [Host]     : .NET 8.0.21 (8.0.2125.47513), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-UCVPBV : .NET 8.0.21 (8.0.2125.47513), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun   : .NET 8.0.21 (8.0.2125.47513), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

WarmupCount=3  

```
| Method                 | Job        | IterationCount | LaunchCount | RunStrategy | Mean     | Error     | StdDev  | Gen0      | Gen1     | Allocated |
|----------------------- |----------- |--------------- |------------ |------------ |---------:|----------:|--------:|----------:|---------:|----------:|
| MixedDocument_200Pages | Job-UCVPBV | 5              | Default     | Throughput  | 150.0 ms |  32.07 ms | 8.33 ms | 3666.6667 | 666.6667 |  22.28 MB |
| MixedDocument_200Pages | ShortRun   | 3              | 1           | Default     | 147.3 ms | 173.93 ms | 9.53 ms | 3666.6667 | 666.6667 |  22.28 MB |
