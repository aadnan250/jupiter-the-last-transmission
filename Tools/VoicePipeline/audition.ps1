param([int]$Count = 120, [int]$Start = 0)
# Generates the same sentence with many Piper speakers and measures each voice's median pitch (F0),
# so voices can be cast by register without listening.
$T = if ($env:JUPITER_TOOLS) { $env:JUPITER_TOOLS } else { Join-Path $PSScriptRoot "../../../_tools" }
$out = Join-Path $T "audition"
New-Item -ItemType Directory -Force $out | Out-Null

$sentence = "Okafor. The calibration numbers look excellent. We are two days ahead of schedule, and I think that matters."
$lines = for ($i = $Start; $i -lt $Start + $Count; $i++) {
    @{ text = $sentence; speaker_id = $i; output_file = (Join-Path $out "s$i.wav") } | ConvertTo-Json -Compress
}
$lines | & (Join-Path $T "piper\piper.exe") --model (Join-Path $T "voices\en_US-libritts_r-medium.onnx") --json-input 2>$null | Out-Null

Add-Type -TypeDefinition @"
using System; using System.IO; using System.Collections.Generic;
public static class Pitch {
    public static double[] Analyze(string path) {
        byte[] b = File.ReadAllBytes(path);
        int sr = BitConverter.ToInt32(b, 24);
        int dataPos = 12;
        while (dataPos < b.Length - 8) {
            string id = System.Text.Encoding.ASCII.GetString(b, dataPos, 4);
            int size = BitConverter.ToInt32(b, dataPos + 4);
            if (id == "data") { dataPos += 8; break; }
            dataPos += 8 + size;
        }
        int n = (b.Length - dataPos) / 2;
        var x = new double[n];
        for (int i = 0; i < n; i++) x[i] = BitConverter.ToInt16(b, dataPos + i * 2) / 32768.0;
        int frame = (int)(0.04 * sr), hop = (int)(0.01 * sr);
        int minLag = sr / 350, maxLag = sr / 65;
        var f0s = new List<double>();
        double peak = 0; foreach (var v in x) peak = Math.Max(peak, Math.Abs(v));
        for (int s = 0; s + frame + maxLag < n; s += hop) {
            double e = 0; for (int i = 0; i < frame; i++) e += x[s + i] * x[s + i];
            if (Math.Sqrt(e / frame) < peak * 0.08) continue;
            double best = 0; int bestLag = 0;
            for (int lag = minLag; lag <= maxLag; lag++) {
                double c = 0, e1 = 0, e2 = 0;
                for (int i = 0; i < frame; i++) { c += x[s + i] * x[s + i + lag]; e1 += x[s+i]*x[s+i]; e2 += x[s+i+lag]*x[s+i+lag]; }
                double r = c / Math.Sqrt(e1 * e2 + 1e-12);
                if (r > best) { best = r; bestLag = lag; }
            }
            if (best > 0.75 && bestLag > 0) f0s.Add((double)sr / bestLag);
        }
        f0s.Sort();
        double med = f0s.Count > 0 ? f0s[f0s.Count / 2] : 0;
        double dur = (double)n / sr;
        return new double[] { med, dur, f0s.Count };
    }
}
"@

$results = Get-ChildItem $out -Filter "s*.wav" | ForEach-Object {
    $id = [int]($_.BaseName.Substring(1))
    $a = [Pitch]::Analyze($_.FullName)
    [pscustomobject]@{ id = $id; f0 = [math]::Round($a[0]); seconds = [math]::Round($a[1], 2); voiced = $a[2] }
} | Sort-Object f0
$results | Export-Csv (Join-Path $T "audition.csv") -NoTypeInformation
$results
