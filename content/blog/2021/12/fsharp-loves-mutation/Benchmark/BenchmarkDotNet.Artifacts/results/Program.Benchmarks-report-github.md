``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.22000
AMD Ryzen 9 3900X, 1 CPU, 24 logical and 12 physical cores
.NET SDK=6.0.100
  [Host]     : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT DEBUG
  DefaultJob : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT


```
|        Method |     Mean |   Error |   StdDev |   Median |
|-------------- |---------:|--------:|---------:|---------:|
|  MutationFree | 292.7 ms | 6.65 ms | 18.10 ms | 285.4 ms |
| MutationBased | 281.8 ms | 5.04 ms |  4.71 ms | 281.5 ms |
