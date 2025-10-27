using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileProviders;
using TypoDukk.QuackView.QuackJob.Data;
using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob.Jobs;

internal interface IJobRunner
{
    string Name { get; }
    string Description { get; }
    Task ExecuteJobFileAsync(string filePath);
    Task CreateNewJobFileAsync(string filePath);
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
internal sealed class JobRunnerAttribute(string name, string description) : Attribute
{
    public string Name { get; set; } = name;
    public string Description { get; set; } = description;
}

internal abstract class JobRunner(IDiskIOService disk) : JobRunner<JobFile>(disk)
{
}

internal abstract class JobRunner<TJobFile>(IDiskIOService disk) : IJobRunner
    where TJobFile : class, new()
{
    public string Name
    {
        get
        {
            var runnerAttribute = this.GetType().GetCustomAttributes<JobRunnerAttribute>().FirstOrDefault();
            return runnerAttribute?.Name ?? throw new InvalidOperationException("JobRunner is missing required JobRunnerAttribute.");
        }
    }

    public string Description
    {
        get
        {
            var runnerAttribute = this.GetType().GetCustomAttributes<JobRunnerAttribute>().FirstOrDefault();
            return runnerAttribute?.Description ?? throw new InvalidOperationException("JobRunner is missing required JobRunnerAttribute.");
        }
    }

    protected IDiskIOService Disk = disk ?? throw new ArgumentNullException(nameof(disk));

    public virtual async Task ExecuteJobFileAsync(string filePath)
    {
        if (!await this.Disk.FileExistsAsync(filePath))
            throw new FileNotFoundException();

        var json = await this.Disk.ReadAllTextAsync(filePath);
        var jobFile = JsonSerializer.Deserialize<TJobFile>(json, Program.DefaultJsonSerializerOptions);

        if (null == jobFile)
            throw new InvalidOperationException("Failed to load job file.");

        await this.ExecuteJobFileAsync(jobFile);
    }


    public abstract Task ExecuteJobFileAsync(TJobFile jobFile);

    public virtual async Task CreateNewJobFileAsync(string filePath)
    {
        var jobFile = new JobFile<TJobFile>()
        {
            Metadata = new JobMetadata()
            {
                Description = "Describe the job here.",
                Runner = this.Name,
            }
        };

        var json = jobFile.ToJson(Program.DefaultJsonSerializerOptions);

        await this.Disk.WriteAllTextAsync(filePath, json);
    }
}