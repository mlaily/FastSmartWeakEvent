Source: https://www.codeproject.com/Articles/29922/Weak-Events-in-C

See the commit history for the list of changes compared to the source (not too much things have been changed).

```
// * Summary *

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.572 (2004/?/20H1)
Intel Core i7-10610U CPU 1.80GHz, 1 CPU, 8 logical and 4 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4250.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4250.0), X64 RyuJIT


|                                 Method |      Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------------------- |----------:|----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
|                'Normal (strong) event' |  12.61 ns |  0.278 ns |  0.522 ns |  1.00 |    0.00 |      - |     - |     - |         - |
|                     'Smart weak event' | 602.10 ns | 10.651 ns | 19.477 ns | 47.89 |    2.85 | 0.0420 |     - |     - |     177 B |
| 'Fast smart weak event (2009 version)' |  41.43 ns |  0.813 ns |  0.799 ns |  3.20 |    0.16 | 0.0172 |     - |     - |      72 B |
| 'Fast smart weak event (2013 version)' |  19.34 ns |  0.189 ns |  0.158 ns |  1.49 |    0.06 |      - |     - |     - |         - |
|                    'Thomas weak event' | 563.14 ns | 10.753 ns | 10.561 ns | 43.42 |    1.53 | 0.1545 |     - |     - |     650 B |
```