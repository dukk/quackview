using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob.Actions;

internal class RunAction(IJobRunnerService jobRunnerService, IConsoleService console) : Action(console)
{
    private readonly IJobRunnerService jobRunner = jobRunnerService ?? throw new ArgumentNullException(nameof(jobRunnerService));

    public override async Task ExecuteAsync(string[] args)
    {
        await jobRunner.RunJobsAsync(args);

        return;
    }
}