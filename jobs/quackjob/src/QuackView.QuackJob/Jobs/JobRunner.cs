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

internal abstract class JobRunner(IFileService file) : JobRunner<JobFile>(file)
{
}

internal abstract class JobRunner<TJobFile>(IFileService file) : IJobRunner
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

    protected IFileService File = file ?? throw new ArgumentNullException(nameof(file));

    public virtual async Task ExecuteJobFileAsync(string filePath)
    {
        if (!await this.File.ExistsAsync(filePath))
            throw new FileNotFoundException();

        var json = await this.File.ReadAllTextAsync(filePath);
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

        await this.File.WriteAllTextAsync(filePath, json);
    }
}