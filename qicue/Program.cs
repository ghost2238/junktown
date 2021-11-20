// https://wiki.hydrogenaud.io/index.php?title=Cue_sheet

string fmt="";
var output = new List<string>();
var track = 1;
var file = string.Join(" ", args);

foreach(var row in File.ReadAllLines(file))
{
    if (row.StartsWith("title"))
    {
        var title = row.Replace("title", "").Trim();
        if (title.Contains(" - "))
        {
            var spl = title.Split(" - ");
            output.Add($"PERFORMER \"{spl[0]}\"");
            output.Add($"TITLE \"{spl[1]}\"");
        }
        else
            output.Add($"TITLE \"{title}\"");
        output.Add($"FILE \"{Path.GetFileNameWithoutExtension(args[0]) + ".mp3"}\" MP3");
    }
    else if (row.StartsWith("fmt"))
        fmt = row.Replace("fmt", "").Trim();
    else
    {
        var spl = row.Split(" ");
        var time = spl[0].Split(":");

        var h = int.Parse(time[0]);
        var m = int.Parse(time[1]);
        var s = int.Parse(time[2]);

        m += h * 60;

        output.Add($"  TRACK {track++.ToString("D2")} AUDIO");
        output.Add($"    TITLE \"{row.Substring(spl[0].Length+1)}\"");
        output.Add($"    PERFORMER \"\"");
        output.Add($"    INDEX 01 {m}:{s}:00");
    }
    
}
File.WriteAllLines(file.Replace(".txt", ".cue"), output.ToArray());