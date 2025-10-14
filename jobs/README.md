# Quack Job

This is the command line tool that provides most of the "standard" out of the box jobs needed to collect the data for the display.

## Usage

`quackjob run <job-type>:<job-config-file>`

### Available job-types

#### Import Upcoming Calendar Events

Job Type:  `upcoming-calendar-events`

Configuration Example:

```json
{
    "daysInFuture": 14,
    "accounts": [
        {
            // Microsoft Account - Only Outlook is supported currently
            "account": "john.doe@example.com", 
            "calendars": ["Family", "School Calendar", "United States holidays"]
        }
    ],
    "outputFileName": "calendar-events.json"
}
```

Example command: `quackjob run upcoming-calendar-events:upcoming-calendar-events-job.json`
