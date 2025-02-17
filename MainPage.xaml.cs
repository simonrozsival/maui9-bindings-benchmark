using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace BenchmarkBindings;

public partial class MainPage : ContentPage
{
	private Task? _task;

	public MainPage()
	{
		InitializeComponent();
	}

	public void OnClick_RunBenchmarks(object sender, EventArgs e)
	{
		_task = Task.Run(RunBenchmarks);
	}

	private void RunBenchmarks()
	{
		var config = ManualConfig.CreateMinimumViable()
			.AddJob(Job.Default.WithToolchain(new InProcessEmitToolchain(TimeSpan.FromMinutes(10), logOutput: true)))
			.AddDiagnoser(MemoryDiagnoser.Default)
			.WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Alphabetical));
		config.UnionRule = ConfigUnionRule.AlwaysUseGlobal; // Overriding the default

		var benchmarks = new[]
		{
			typeof(CreateBinding),
			typeof(SetBindingNoContext),
			typeof(SetBinding),
			typeof(UpdateValueOneLevel),
			typeof(UpdateValueTwoLevels),
		};

		BenchmarkRunner.Run(benchmarks, config.WithOptions(ConfigOptions.DisableLogFile));
	}
}

