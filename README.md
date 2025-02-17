# Benchmarking classing string-based bindings vs. compiled bindings in .NET MAUI 9

All the results on this page were collected on Samsung Galaxy S23 using these commands:
```
dotnet build -c Release -t:Run -f net9.0-android
adb shell logcat -d -v tag -s "DOTNET" > android-results.txt
```

## 1. Creating new binding

| Method          | Mean     | Error     | StdDev    | Ratio | Gen0   | Gen1   | Gen2   | Allocated | Alloc Ratio |
|---------------- |---------:|----------:|----------:|------:|-------:|-------:|-------:|----------:|------------:|
| Compiled_Create | 2.899 us | 0.0485 us | 0.0454 us |  0.78 | 0.2022 | 0.0343 | 0.0343 |     960 B |        0.83 |
| Classic_Create  | 3.704 us | 0.0194 us | 0.0182 us |  1.00 | 0.2327 | 0.0458 | 0.0458 |    1152 B |        1.00 |

<details>
<summary>Source code</summary>

```c#
[MemoryDiagnoser(false)]
public class CreateBinding
{
    [Benchmark(Baseline = true)]
    public void Classic_Create()
    {
        _ = new Binding("FullName.FirstName");
    }

    [Benchmark]
    public void Compiled_Create()
    {
        _ = Binding.Create(static (ContactInformation contact) => contact.FullName?.FirstName);
    }
}
```
</details>

## 2. SetBinding - without existing BindingContext

| Method              | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------- |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| Compiled_SetBinding | 26.04 us | 0.539 us | 1.493 us |  0.76 |    0.05 |         - |          NA |
| Classic_SetBinding  | 34.45 us | 0.701 us | 1.400 us |  1.00 |    0.06 |         - |          NA |

<details>
<summary>Source code</summary>

```c#
[MemoryDiagnoser(false)]
public class SetBindingNoContext
{
    Label _label = new();

    [Benchmark(Baseline = true)]
    public void Classic_SetBinding()
    {
        _label.SetBinding(Label.TextProperty, "FullName.FirstName");
    }

    [Benchmark]
    public void Compiled_SetBinding()
    {
        _label.SetBinding(Label.TextProperty, static (ContactInformation contact) => contact.FullName?.FirstName);
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _label.RemoveBinding(Label.TextProperty);
    }
}
```

</details>

## 3. SetBinding - with existing BindingContext

| Method              | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------- |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| Compiled_SetBinding | 30.61 us | 0.629 us | 1.182 us |  0.45 |    0.02 |         - |          NA |
| Classic_SetBinding  | 67.81 us | 1.338 us | 1.787 us |  1.00 |    0.04 |         - |          NA |

<details>
<summary>Source code</summary>

```c#
[MemoryDiagnoser(false)]
public class SetBinding
{
    private readonly ContactInformation _contact = new(new FullName("John"));
    private readonly Label _label = new();

    [GlobalSetup]
    public void Setup()
    {
        DispatcherProvider.SetCurrent(new MockDispatcherProvider());
        _label.BindingContext = _contact;
    }

    [Benchmark(Baseline = true)]
    public void Classic_SetBinding()
    {
        _label.SetBinding(Label.TextProperty, "FullName.FirstName");
    }

    [Benchmark]
    public void Compiled_SetBinding()
    {
        _label.SetBinding(Label.TextProperty, static (ContactInformation contact) => contact.FullName?.FirstName);
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _label.RemoveBinding(Label.TextProperty);
    }
}
```
</details>

## 4. Update source value - single nesting level

| Method                           | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------------- |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| Compiled_UpdateWhenSourceChanges | 30.78 us | 0.631 us | 1.512 us |  0.71 |    0.04 |         - |          NA |
| Classic_UpdateWhenSourceChanges  | 43.12 us | 0.870 us | 1.500 us |  1.00 |    0.05 |         - |          NA |

<details>
<summary>source code</summary>

```c#
[MemoryDiagnoser(false)]
public class UpdateValueOneLevel
{
    private readonly FullName _name = new("John");
    private readonly Label _label = new();

    [GlobalSetup]
    public void Setup()
    {
        DispatcherProvider.SetCurrent(new MockDispatcherProvider());
        _label.BindingContext = _name;
    }

    [IterationCleanup]
    public void Reset()
    {
        _label.Text = "John";
        _name.FirstName = "John";
        _label.RemoveBinding(Label.TextProperty);
    }

    [IterationSetup(Target = nameof(Classic_UpdateWhenSourceChanges))]
    public void SetupClassicBinding()
    {
        _label.SetBinding(Label.TextProperty, "FirstName");
    }

    [IterationSetup(Target = nameof(Compiled_UpdateWhenSourceChanges))]
    public void SetupCompiledBinding()
    {
        _label.SetBinding(Label.TextProperty, static (FullName name) => name.FirstName);
    }

    [Benchmark(Baseline = true)]
    public void Classic_UpdateWhenSourceChanges()
    {
        _name.FirstName = "Jane";
    }

    [Benchmark]
    public void Compiled_UpdateWhenSourceChanges()
    {
        _name.FirstName = "Jane";
    }
}
```

</details>

## 5. Update source value - two nesting levels

| Method                           | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------------- |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| Compiled_UpdateWhenSourceChanges | 30.85 us | 0.634 us | 1.295 us |  0.67 |    0.03 |         - |          NA |
| Classic_UpdateWhenSourceChanges  | 46.06 us | 0.934 us | 1.369 us |  1.00 |    0.04 |         - |          NA |

<details>
<summary>Source code</summary>

```c#
[MemoryDiagnoser(false)]
public class UpdateValueTwoLevels
{
    ContactInformation _contact = new(new FullName("John"));
    Label _label = new();

    [GlobalSetup]
    public void Setup()
    {
        DispatcherProvider.SetCurrent(new MockDispatcherProvider());
        _label.BindingContext = _contact;
    }

    [IterationCleanup]
    public void Reset()
    {
        _label.Text = "John";
        _contact.FullName.FirstName = "John";
        _label.RemoveBinding(Label.TextProperty);
    }

    [IterationSetup(Target = nameof(Classic_UpdateWhenSourceChanges))]
    public void SetupClassicBinding()
    {
        _label.SetBinding(Label.TextProperty, "FullName.FirstName");
    }

    [IterationSetup(Target = nameof(Compiled_UpdateWhenSourceChanges))]
    public void SetupCompiledBinding()
    {
        _label.SetBinding(Label.TextProperty, static (ContactInformation contact) => contact.FullName?.FirstName);
    }

    [Benchmark(Baseline = true)]
    public void Classic_UpdateWhenSourceChanges()
    {
        _contact.FullName.FirstName = "Jane";
    }

    [Benchmark]
    public void Compiled_UpdateWhenSourceChanges()
    {
        _contact.FullName.FirstName = "Jane";
    }
}
```

</details>

---

See [`Benchmarks.cs`](./Benchmarks.cs) for full source code.