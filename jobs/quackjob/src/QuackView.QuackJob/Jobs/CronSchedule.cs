// using System;
// using System.Collections.Generic;
// using System.Globalization;

namespace TypoDukk.QuackView.QuackJob.Jobs;

// internal sealed class CronSchedule
// {
//     public string Cron { get; }
//     public CronField Seconds { get; }
//     public CronField Minutes { get; }
//     public CronField Hours { get; }
//     public CronField DayOfMonth { get; }
//     public CronField Month { get; }
//     public CronField DayOfWeek { get; }

//     private CronSchedule(
//         string cron,
//         CronField seconds,
//         CronField minutes,
//         CronField hours,
//         CronField dayOfMonth,
//         CronField month,
//         CronField dayOfWeek)
//     {
//         Cron = cron;
//         Seconds = seconds;
//         Minutes = minutes;
//         Hours = hours;
//         DayOfMonth = dayOfMonth;
//         Month = month;
//         DayOfWeek = dayOfWeek;
//     }

//     public static CronSchedule Parse(string cron)
//     {
//         if (!TryParse(cron, out var schedule, out var error))
//             throw new FormatException(error ?? "Invalid cron expression.");
//         return schedule!;
//     }

//     public static bool TryParse(string cron, out CronSchedule? schedule, out string? error)
//     {
//         schedule = null;
//         error = null;

//         if (cron is null) { error = "Cron expression is null."; return false; }

//         var parts = SplitFields(cron);
//         if (parts.Length != 5 && parts.Length != 6)
//         {
//             error = "Cron must have 5 or 6 fields (with optional seconds).";
//             return false;
//         }

//         // If 5 fields, assume 0 seconds.
//         var hasSeconds = parts.Length == 6;

//         var secExpr = hasSeconds ? parts[0] : "0";
//         var minExpr = hasSeconds ? parts[1] : parts[0];
//         var hrExpr  = hasSeconds ? parts[2] : parts[1];
//         var domExpr = hasSeconds ? parts[3] : parts[2];
//         var monExpr = hasSeconds ? parts[4] : parts[3];
//         var dowExpr = hasSeconds ? parts[5] : parts[4];

//         // Normalize aliases (JAN, MON, etc) and special '?'
//         monExpr = NormalizeMonthAliases(monExpr);
//         dowExpr = NormalizeDayOfWeekAliases(dowExpr);

//         if (!CronField.TryParse(secExpr, 0, 59, false, out var secField, out error)) return false;
//         if (!CronField.TryParse(minExpr, 0, 59, false, out var minField, out error)) return false;
//         if (!CronField.TryParse(hrExpr, 0, 23, false, out var hrField, out error)) return false;
//         if (!CronField.TryParse(domExpr, 1, 31, false, out var domField, out error)) return false;
//         if (!CronField.TryParse(monExpr, 1, 12, false, out var monField, out error)) return false;
//         if (!CronField.TryParse(dowExpr, 0, 6, true, out var dowField, out error)) return false;

//         var normalized = $"{secField!.Expression} {minField!.Expression} {hrField!.Expression} {domField!.Expression} {monField!.Expression} {dowField!.Expression}";
//         schedule = new CronSchedule(normalized, secField, minField, hrField, domField, monField, dowField);
//         return true;
//     }

//     public string ToCron() => Cron;

//     public string ToHumanString(CultureInfo? culture = null)
//     {
//         var ci = culture ?? CultureInfo.CurrentCulture;
//         var dtf = ci.DateTimeFormat;

//         // Simplest: every N seconds/minutes
//         if (Seconds.IsEvery && Minutes.IsEvery && Hours.IsEvery && DayOfMonth.IsEvery && Month.IsEvery && DayOfWeek.IsEvery)
//             return "Every second";

//         if (Seconds.IsEveryN && Minutes.IsEvery && Hours.IsEvery && DayOfMonth.IsEvery && Month.IsEvery && DayOfWeek.IsEvery)
//             return Seconds.Step == 1 ? "Every second" : $"Every {Seconds.Step} seconds";

//         if (Minutes.IsEvery && Hours.IsEvery && DayOfMonth.IsEvery && Month.IsEvery && DayOfWeek.IsEvery && Seconds.IsSingle(0))
//             return "Every minute";

//         if (Minutes.IsEveryN && Hours.IsEvery && DayOfMonth.IsEvery && Month.IsEvery && DayOfWeek.IsEvery && Seconds.IsSingle(0))
//             return Minutes.Step == 1 ? "Every minute" : $"Every {Minutes.Step} minutes";

//         // Time phrase
//         var timePhrase = BuildTimePhrase(dtf, Seconds, Minutes, Hours);

//         // If both DOM and DOW are set, it's ambiguous across cron variants. Fallback.
//         var domConstrained = !DayOfMonth.IsEvery;
//         var dowConstrained = !DayOfWeek.IsEvery;

//         // Month phrase
//         string monthPhrase = "";
//         if (!Month.IsEvery)
//         {
//             var months = ValuesToMonthNames(dtf, Month.GetValues());
//             monthPhrase = months.Length > 0 ? $" in {JoinWithComma(months)}" : "";
//         }

//         if (domConstrained && dowConstrained)
//             return $"Cron: {Cron}";

//         if (dowConstrained)
//         {
//             var days = ValuesToDayNames(dtf, DayOfWeek.GetValues());
//             var when = timePhrase.Length > 0 ? $" {timePhrase}" : "";
//             return $"Every {JoinWithComma(days)}{monthPhrase}{when}";
//         }

//         if (domConstrained)
//         {
//             var doms = IntsToStrings(DayOfMonth.GetValues());
//             var when = timePhrase.Length > 0 ? $" {timePhrase}" : "";
//             var monthScope = Month.IsEvery ? "every month" : JoinWithComma(ValuesToMonthNames(dtf, Month.GetValues()));
//             return $"On day {(doms.Length == 1 ? doms[0] : string.Join(",", doms))} of {(Month.IsEvery ? monthScope : "")}{(Month.IsEvery ? "" : monthScope)}{when}".Replace("  ", " ").Trim();
//         }

//         // Only month constrained
//         if (!Month.IsEvery)
//         {
//             var when = timePhrase.Length > 0 ? $" {timePhrase}" : "";
//             return $"Every day{monthPhrase}{when}".Trim();
//         }

//         // Default daily phrasing
//         if (timePhrase.Length > 0)
//             return $"Every day {timePhrase}".Trim();

//         return $"Cron: {Cron}";
//     }

//     private static string[] ValuesToDayNames(DateTimeFormatInfo dtf, int[] values)
//     {
//         var names = new List<string>(values.Length);
//         for (int i = 0; i < values.Length; i++)
//         {
//             var v = values[i];
//             if (v < 0 || v > 6) continue;
//             var name = dtf.AbbreviatedDayNames[(v + 0) % 7];
//             names.Add(name);
//         }
//         return names.ToArray();
//     }

//     private static string[] ValuesToMonthNames(DateTimeFormatInfo dtf, int[] values)
//     {
//         var names = new List<string>(values.Length);
//         for (int i = 0; i < values.Length; i++)
//         {
//             var v = values[i];
//             if (v < 1 || v > 12) continue;
//             var name = dtf.AbbreviatedMonthNames[v - 1];
//             if (!string.IsNullOrEmpty(name)) names.Add(name);
//         }
//         return names.ToArray();
//     }

//     private static string[] IntsToStrings(int[] values)
//     {
//         var s = new string[values.Length];
//         for (int i = 0; i < values.Length; i++) s[i] = values[i].ToString(CultureInfo.InvariantCulture);
//         return s;
//     }

//     private static string BuildTimePhrase(DateTimeFormatInfo dtf, CronField seconds, CronField minutes, CronField hours)
//     {
//         // Seconds-only phrases
//         if (!seconds.IsEvery && minutes.IsEvery && hours.IsEvery)
//         {
//             if (seconds.IsEveryN)
//                 return seconds.Step == 1 ? "" : $"every {seconds.Step} seconds";
//             if (seconds.IsSingle())
//                 return seconds.IsSingle(0) ? "" : $"at {Pad2(seconds.GetValues()[0])} seconds past the minute";
//         }

//         // Minutes/Hrs
//         if (minutes.IsSingle() && hours.IsSingle())
//         {
//             return $"at {FormatTime(dtf, hours.GetValues()[0], minutes.GetValues()[0])}";
//         }

//         if (minutes.IsSingle() && hours.IsEvery)
//         {
//             return $"at {Pad2(minutes.GetValues()[0])} past every hour";
//         }

//         if (minutes.IsEvery && hours.IsSingle())
//         {
//             var h = hours.GetValues()[0];
//             return $"every minute during {Pad2(h)}:00 hour";
//         }

//         if (minutes.IsEveryN && hours.IsEvery)
//         {
//             return minutes.Step == 1 ? "every minute" : $"every {minutes.Step} minutes";
//         }

//         return ""; // too complex
//     }

//     private static string FormatTime(DateTimeFormatInfo dtf, int hour24, int minute)
//     {
//         // Respect current culture pattern by constructing a DateTime and formatting
//         var dt = new DateTime(2000, 1, 1, hour24, minute, 0);
//         return dt.ToString(dtf.ShortTimePattern, dtf);
//     }

//     private static string Pad2(int v) => v.ToString("00", CultureInfo.InvariantCulture);

//     private static string JoinWithComma(string[] parts)
//     {
//         if (parts.Length == 0) return "";
//         if (parts.Length == 1) return parts[0];
//         var sb = new System.Text.StringBuilder();
//         for (int i = 0; i < parts.Length; i++)
//         {
//             if (i > 0) sb.Append(i == parts.Length - 1 ? " and " : ", ");
//             sb.Append(parts[i]);
//         }
//         return sb.ToString();
//     }

//     private static string[] SplitFields(string cron)
//     {
//         var raw = cron.Trim();
//         var list = new List<string>(6);
//         int i = 0;
//         while (i < raw.Length)
//         {
//             // skip whitespace
//             while (i < raw.Length && char.IsWhiteSpace(raw[i])) i++;
//             if (i >= raw.Length) break;
//             int start = i;
//             while (i < raw.Length && !char.IsWhiteSpace(raw[i])) i++;
//             list.Add(raw.Substring(start, i - start));
//         }
//         return list.ToArray();
//     }

//     private static string NormalizeMonthAliases(string expr)
//     {
//         if (string.IsNullOrWhiteSpace(expr)) return expr;
//         var up = expr.Trim().ToUpperInvariant();
//         up = ReplaceAliases(up, new Dictionary<string, string>
//         {
//             { "JAN", "1" }, { "FEB", "2" }, { "MAR", "3" }, { "APR", "4" },
//             { "MAY", "5" }, { "JUN", "6" }, { "JUL", "7" }, { "AUG", "8" },
//             { "SEP", "9" }, { "OCT", "10" }, { "NOV", "11" }, { "DEC", "12" }
//         });
//         up = up.Replace("?", "*", StringComparison.Ordinal);
//         return up;
//     }

//     private static string NormalizeDayOfWeekAliases(string expr)
//     {
//         if (string.IsNullOrWhiteSpace(expr)) return expr;
//         var up = expr.Trim().ToUpperInvariant();
//         up = ReplaceAliases(up, new Dictionary<string, string>
//         {
//             { "SUN", "0" }, { "MON", "1" }, { "TUE", "2" }, { "WED", "3" },
//             { "THU", "4" }, { "FRI", "5" }, { "SAT", "6" }
//         });
//         up = up.Replace("?", "*", StringComparison.Ordinal);
//         return up;
//     }

//     private static string ReplaceAliases(string input, IDictionary<string, string> map)
//     {
//         // Replace aliases when they appear as whole tokens or within commas/ranges/steps.
//         // We do a simple pass over letters to find tokens.
//         var sb = new System.Text.StringBuilder(input.Length);
//         var token = new System.Text.StringBuilder();
//         for (int i = 0; i < input.Length; i++)
//         {
//             char ch = input[i];
//             bool isAlpha = (ch >= 'A' && ch <= 'Z');
//             if (isAlpha)
//             {
//                 token.Append(ch);
//             }
//             else
//             {
//                 if (token.Length > 0)
//                 {
//                     var key = token.ToString();
//                     if (map.TryGetValue(key, out var val)) sb.Append(val);
//                     else sb.Append(key);
//                     token.Clear();
//                 }
//                 sb.Append(ch);
//             }
//         }
//         if (token.Length > 0)
//         {
//             var key = token.ToString();
//             if (map.TryGetValue(key, out var val)) sb.Append(val);
//             else sb.Append(key);
//         }
//         return sb.ToString();
//     }
// }

// internal sealed class CronField
// {
//     public string Expression { get; }
//     public int Min { get; }
//     public int Max { get; }
//     public bool IsEvery { get; }
//     public bool IsEveryN { get; }
//     public int Step { get; }
//     private readonly int[]? _values; // null => every

//     private CronField(string expression, int min, int max, bool isEvery, bool isEveryN, int step, int[]? values)
//     {
//         Expression = expression;
//         Min = min;
//         Max = max;
//         IsEvery = isEvery;
//         IsEveryN = isEveryN;
//         Step = step;
//         _values = values;
//     }

//     public bool IsSingle() => _values is { Length: 1 };
//     public bool IsSingle(int value) => _values is { Length: 1 } && _values[0] == value;
//     public int[] GetValues() => _values ?? Array.Empty<int>();

//     public static bool TryParse(string expr, int min, int max, bool mapSunday7To0, out CronField? field, out string? error)
//     {
//         field = null;
//         error = null;

//         if (expr is null) { error = "Null field."; return false; }
//         var raw = expr.Trim();
//         if (raw.Length == 0) { error = "Empty field."; return false; }

//         var up = raw.ToUpperInvariant();
//         if (up == "?") up = "*";

//         // Every step like */n
//         if (up.Length > 2 && up[0] == '*' && up[1] == '/')
//         {
//             if (!int.TryParse(up.Substring(2), NumberStyles.Integer, CultureInfo.InvariantCulture, out var step) || step <= 0)
//             {
//                 error = $"Invalid step in field '{expr}'.";
//                 return false;
//             }
//             var values = ExpandEvery(min, max, step);
//             field = new CronField(up, min, max, false, true, step, values);
//             return true;
//         }

//         if (up == "*")
//         {
//             field = new CronField("*", min, max, true, false, 0, null);
//             return true;
//         }

//         // Expand lists and ranges
//         var set = new HashSet<int>();
//         var token = new System.Text.StringBuilder();
//         for (int i = 0; i <= up.Length; i++)
//         {
//             if (i == up.Length || up[i] == ',')
//             {
//                 if (token.Length == 0) { error = $"Invalid list in field '{expr}'."; return false; }
//                 if (!ExpandToken(token.ToString(), min, max, set, mapSunday7To0, out error)) return false;
//                 token.Clear();
//             }
//             else
//             {
//                 token.Append(up[i]);
//             }
//         }

//         if (set.Count == 0) { error = $"No values in field '{expr}'."; return false; }
//         var valuesArr = new int[set.Count];
//         set.CopyTo(valuesArr);
//         Array.Sort(valuesArr);

//         // Normalize Sunday 7->0 already handled in ExpandToken
//         field = new CronField(up, min, max, false, false, 0, valuesArr);
//         return true;
//     }

//     private static int[] ExpandEvery(int min, int max, int step)
//     {
//         var list = new List<int>();
//         for (int v = min; v <= max; v += step) list.Add(v);
//         return list.ToArray();
//     }

//     private static bool ExpandToken(string token, int min, int max, HashSet<int> set, bool mapSunday7To0, out string? error)
//     {
//         error = null;

//         // step forms: a-b/n or a/n
//         var slashIdx = token.IndexOf('/');
//         string rangePart = token;
//         int step = 1;
//         if (slashIdx >= 0)
//         {
//             rangePart = token.Substring(0, slashIdx);
//             var stepPart = token.Substring(slashIdx + 1);
//             if (!int.TryParse(stepPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out step) || step <= 0)
//             {
//                 error = $"Invalid step '{stepPart}' in '{token}'.";
//                 return false;
//             }
//         }

//         if (rangePart == "*")
//         {
//             for (int v = min; v <= max; v += step) set.Add(v);
//             return true;
//         }

//         int dashIdx = rangePart.IndexOf('-');
//         if (dashIdx >= 0)
//         {
//             var aStr = rangePart.Substring(0, dashIdx);
//             var bStr = rangePart.Substring(dashIdx + 1);
//             if (!TryParseInt(aStr, out var a) || !TryParseInt(bStr, out var b))
//             {
//                 error = $"Invalid range '{rangePart}'.";
//                 return false;
//             }
//             if (mapSunday7To0)
//             {
//                 if (a == 7) a = 0;
//                 if (b == 7) b = 0;
//             }
//             if (a <= b)
//             {
//                 for (int v = a; v <= b; v += step)
//                 {
//                     if (v < min || v > max) { error = $"Value {v} out of range {min}-{max}."; return false; }
//                     set.Add(v);
//                 }
//             }
//             else
//             {
//                 // wrap-around range (rare, but handle): e.g., 6-1
//                 for (int v = a; v <= max; v += step)
//                 {
//                     if (v < min || v > max) { error = $"Value {v} out of range {min}-{max}."; return false; }
//                     set.Add(v);
//                 }
//                 for (int v = min; v <= b; v += step)
//                 {
//                     if (v < min || v > max) { error = $"Value {v} out of range {min}-{max}."; return false; }
//                     set.Add(v);
//                 }
//             }
//             return true;
//         }

//         // Single value with optional step (a/n means a..max by step)
//         if (!TryParseInt(rangePart, out var single))
//         {
//             error = $"Invalid token '{token}'.";
//             return false;
//         }
//         if (mapSunday7To0 && single == 7) single = 0;
//         if (single < min || single > max)
//         {
//             error = $"Value {single} out of range {min}-{max}.";
//             return false;
//         }
//         if (step == 1)
//         {
//             set.Add(single);
//         }
//         else
//         {
//             for (int v = single; v <= max; v += step) set.Add(v);
//         }
//         return true;
//     }

//     private static bool TryParseInt(string s, out int value)
//         => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
// }