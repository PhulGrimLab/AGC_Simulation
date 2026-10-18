using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Agc.Wpf.Scene;

/// <summary>그릴 천체.</summary>
internal enum PlanetKind
{
    Earth,
    Moon,
}

/// <summary>
/// 등장방형(equirectangular) 텍스처 한 장. 임베디드 리소스의 JPEG를 BGRA 바이트 배열로 풀어 둔다.
/// 불변이며 여러 스레드가 읽어도 안전하다(Parallel 렌더링에서 공유).
/// </summary>
internal sealed class PlanetTexture
{
    private PlanetTexture(int width, int height, byte[] bgra)
    {
        Width = width;
        Height = height;
        Bgra = bgra;
    }

    public int Width { get; }

    public int Height { get; }

    public byte[] Bgra { get; }

    /// <summary>어셈블리 임베디드 리소스에서 텍스처를 읽는다(Application 없이도 동작).</summary>
    public static PlanetTexture Load(string resourceName)
    {
        var assembly = typeof(PlanetTexture).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"텍스처 리소스를 찾을 수 없다: {resourceName}");
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = new FormatConvertedBitmap(decoder.Frames[0], PixelFormats.Bgra32, null, 0);
        var stride = frame.PixelWidth * 4;
        var pixels = new byte[stride * frame.PixelHeight];
        frame.CopyPixels(pixels, stride, 0);
        return new PlanetTexture(frame.PixelWidth, frame.PixelHeight, pixels);
    }
}

/// <summary>
/// 구 모양 천체(지구/달)를 텍스처로 그려 비트맵으로 만드는 소프트웨어 렌더러.
/// - 직교투영의 역변환으로 화면 픽셀 → 위·경도를 구해 텍스처를 쌍선형 보간한다.
/// - 조명: 태양 방향에 따른 램버트 음영과 부드러운 터미네이터(명암 경계).
///   지구는 바다 반사광(스펙큘러), 구름층(절차적 노이즈), 대기광(림 + 바깥 글로)을 더하고,
///   달은 림 다크닝과 약한 지구광을 더한다.
/// - 결과 비트맵은 한 변이 discDiameter × <see cref="FrameScale"/> 인 정사각형이며 천체 원반은 그 중앙에 있다
///   (여백은 대기 글로가 차지한다). 호출 쪽은 이 비트맵을 천체 중심에 맞춰 그리면 된다.
/// - 스레드: 정적 유틸리티. 텍스처는 처음 쓸 때 한 번 읽어 공유하며, 행 단위 Parallel.For로 계산한다.
/// </summary>
internal static class PlanetRenderer
{
    /// <summary>원반 지름 대비 결과 비트맵 한 변의 배율(대기 글로 여백 포함).</summary>
    public const double FrameScale = 1.24;

    private const string EarthResource = "Agc.Wpf.Assets.earth_2048.jpg";
    private const string MoonResource = "Agc.Wpf.Assets.moon_1024.jpg";
    private const int CloudMapWidth = 512;
    private const int CloudMapHeight = 256;
    private const double GlowThickness = 0.16;
    private const double EarthAmbient = 0.035;
    private const double MoonAmbient = 0.025;
    private const double TerminatorStart = -0.10;
    private const double TerminatorEnd = 0.38;
    private const double RadToDeg = 180.0 / Math.PI;
    private const double DegToRad = Math.PI / 180.0;

    private static readonly Lazy<PlanetTexture> EarthTexture = new(() => PlanetTexture.Load(EarthResource));
    private static readonly Lazy<PlanetTexture> MoonTexture = new(() => PlanetTexture.Load(MoonResource));
    private static readonly Lazy<float[]> CloudMap = new(BuildCloudMap);

    /// <summary>
    /// 천체를 그린다.
    /// </summary>
    /// <param name="kind">지구 또는 달.</param>
    /// <param name="discDiameter">원반 지름(픽셀).</param>
    /// <param name="centerLongitudeDeg">화면 중앙에 보이는 경도(자전/시점).</param>
    /// <param name="centerLatitudeDeg">화면 중앙에 보이는 위도(기울임).</param>
    /// <param name="sunAzimuthDeg">태양 방향의 화면 평면 각도(오른쪽=0°, 반시계).</param>
    /// <param name="sunElevationDeg">태양이 화면 평면에서 관측자 쪽으로 들린 각도(90°=정면 보름, 음수=뒤쪽 초승).</param>
    /// <param name="cloudShiftDeg">구름층이 표면에 대해 어긋난 경도(구름 흐름).</param>
    public static BitmapSource Render(
        PlanetKind kind, int discDiameter, double centerLongitudeDeg, double centerLatitudeDeg,
        double sunAzimuthDeg, double sunElevationDeg, double cloudShiftDeg)
    {
        var size = (int)Math.Ceiling(discDiameter * FrameScale);
        var pixels = new byte[size * size * 4];
        var texture = kind == PlanetKind.Earth ? EarthTexture.Value : MoonTexture.Value;
        var clouds = kind == PlanetKind.Earth ? CloudMap.Value : null;

        var radius = discDiameter / 2.0;
        var center = size / 2.0;
        var lat0 = centerLatitudeDeg * DegToRad;
        var lon0 = centerLongitudeDeg * DegToRad;
        var sinLat0 = Math.Sin(lat0);
        var cosLat0 = Math.Cos(lat0);
        var az = sunAzimuthDeg * DegToRad;
        var el = sunElevationDeg * DegToRad;
        var sun = (X: Math.Cos(el) * Math.Cos(az), Y: Math.Cos(el) * Math.Sin(az), Z: Math.Sin(el));
        var cloudShift = cloudShiftDeg * DegToRad;

        Parallel.For(0, size, row =>
        {
            for (var col = 0; col < size; col++)
            {
                var dx = (col + 0.5 - center) / radius;
                var dy = (center - (row + 0.5)) / radius;
                var r2 = dx * dx + dy * dy;
                var offset = (row * size + col) * 4;
                if (r2 <= 1.0)
                {
                    ShadeDisc(kind, texture, clouds, pixels, offset, dx, dy, r2, radius, lon0, sinLat0, cosLat0, sun, cloudShift);
                }
                else if (kind == PlanetKind.Earth)
                {
                    ShadeGlow(pixels, offset, dx, dy, r2, sun);
                }
                else
                {
                    // 달은 대기가 없으므로 원반 밖은 투명하다.
                }
            }
        });

        var bitmap = BitmapSource.Create(size, size, 96, 96, PixelFormats.Pbgra32, null, pixels, size * 4);
        bitmap.Freeze();
        return bitmap;
    }

    private static void ShadeDisc(
        PlanetKind kind, PlanetTexture texture, float[]? clouds, byte[] pixels, int offset,
        double nx, double ny, double r2, double radius, double lon0, double sinLat0, double cosLat0,
        (double X, double Y, double Z) sun, double cloudShift)
    {
        var nz = Math.Sqrt(Math.Max(0.0, 1.0 - r2));
        var lat = Math.Asin(Math.Clamp(ny * cosLat0 + nz * sinLat0, -1.0, 1.0));
        var lon = lon0 + Math.Atan2(nx, nz * cosLat0 - ny * sinLat0);
        var u = (lon / (2 * Math.PI) + 0.5) % 1.0;
        if (u < 0)
        {
            u += 1.0;
        }

        var v = 0.5 - lat / Math.PI;
        Sample(texture, u, v, out var tr, out var tg, out var tb);

        var ndl = nx * sun.X + ny * sun.Y + nz * sun.Z;
        var day = SmoothStep(TerminatorStart, TerminatorEnd, ndl);
        var ambient = kind == PlanetKind.Earth ? EarthAmbient : MoonAmbient;
        var light = ambient + (1.0 - ambient) * day;

        double r;
        double g;
        double b;
        if (kind == PlanetKind.Earth)
        {
            // 구름: 표면과 다른 속도로 흐르는 얇은 층(흰색, 밤에는 어둡게)
            var cloud = SampleCloud(clouds!, u + cloudShift / (2 * Math.PI), v);
            var ocean = tb > tr * 1.5 && tb > tg * 1.25 ? 1.0 : 0.0;
            var reflect = 2.0 * ndl * nz - sun.Z;
            var spec = ocean * Math.Pow(Math.Max(0.0, reflect), 36.0) * 0.55 * day;
            var cr = tr / 255.0 * (1 - cloud) + cloud * 0.96;
            var cg = tg / 255.0 * (1 - cloud) + cloud * 0.96;
            var cb = tb / 255.0 * (1 - cloud) + cloud * 0.98;
            // 대기광: 가장자리로 갈수록 푸르게(밝은 쪽에서 더 강하게)
            var rim = Math.Pow(1.0 - nz, 2.6) * (0.25 + 0.75 * day) * 0.95;
            r = cr * light + spec + rim * 0.30;
            g = cg * light + spec + rim * 0.55;
            b = cb * light + spec + rim * 1.00;
        }
        else
        {
            // 달: 림 다크닝(가장자리가 약간 어둡다)과 약한 대비 강화
            var limb = 0.70 + 0.30 * nz;
            var gain = light * limb * 1.12;
            r = tr / 255.0 * gain;
            g = tg / 255.0 * gain;
            b = tb / 255.0 * gain * 0.98;
        }

        // 원반 가장자리 안티앨리어싱(1픽셀 폭)
        var coverage = Math.Clamp((1.0 - Math.Sqrt(r2)) * radius + 0.5, 0.0, 1.0);
        WritePremultiplied(pixels, offset, r, g, b, coverage);
    }

    /// <summary>지구 원반 밖의 대기 글로: 태양을 향한 쪽이 더 밝다.</summary>
    private static void ShadeGlow(byte[] pixels, int offset, double dx, double dy, double r2, (double X, double Y, double Z) sun)
    {
        var dist = Math.Sqrt(r2);
        var t = (dist - 1.0) / GlowThickness;
        if (t >= 1.0)
        {
            return;
        }

        var facing = (dx * sun.X + dy * sun.Y) / dist;
        var sunSide = Math.Clamp(0.30 + 0.70 * facing + 0.35 * sun.Z, 0.0, 1.0);
        var glow = Math.Pow(1.0 - t, 2.4) * 0.60 * sunSide;
        WritePremultiplied(pixels, offset, 0.32, 0.58, 1.0, glow);
    }

    private static void WritePremultiplied(byte[] pixels, int offset, double r, double g, double b, double alpha)
    {
        var a = Math.Clamp(alpha, 0.0, 1.0);
        pixels[offset] = ToByte(Math.Min(b, 1.0) * a);
        pixels[offset + 1] = ToByte(Math.Min(g, 1.0) * a);
        pixels[offset + 2] = ToByte(Math.Min(r, 1.0) * a);
        pixels[offset + 3] = ToByte(a);
    }

    private static byte ToByte(double value)
    {
        return (byte)Math.Clamp((int)Math.Round(value * 255.0), 0, 255);
    }

    private static double SmoothStep(double edge0, double edge1, double x)
    {
        var t = Math.Clamp((x - edge0) / (edge1 - edge0), 0.0, 1.0);
        return t * t * (3 - 2 * t);
    }

    /// <summary>텍스처 쌍선형 샘플링(u는 감싸고 v는 가장자리 고정).</summary>
    private static void Sample(PlanetTexture t, double u, double v, out double r, out double g, out double b)
    {
        var x = u * t.Width - 0.5;
        var y = Math.Clamp(v * t.Height - 0.5, 0.0, t.Height - 1.001);
        var x0 = (int)Math.Floor(x);
        var y0 = (int)Math.Floor(y);
        var fx = x - x0;
        var fy = y - y0;
        var xa = ((x0 % t.Width) + t.Width) % t.Width;
        var xb = (xa + 1) % t.Width;
        var ya = y0;
        var yb = Math.Min(y0 + 1, t.Height - 1);

        r = 0;
        g = 0;
        b = 0;
        Accumulate(t, xa, ya, (1 - fx) * (1 - fy), ref r, ref g, ref b);
        Accumulate(t, xb, ya, fx * (1 - fy), ref r, ref g, ref b);
        Accumulate(t, xa, yb, (1 - fx) * fy, ref r, ref g, ref b);
        Accumulate(t, xb, yb, fx * fy, ref r, ref g, ref b);
    }

    private static void Accumulate(PlanetTexture t, int x, int y, double weight, ref double r, ref double g, ref double b)
    {
        var i = (y * t.Width + x) * 4;
        b += t.Bgra[i] * weight;
        g += t.Bgra[i + 1] * weight;
        r += t.Bgra[i + 2] * weight;
    }

    // ---- 구름 지도(절차적 노이즈, 한 번 만들어 공유) ----

    private static float[] BuildCloudMap()
    {
        var map = new float[CloudMapWidth * CloudMapHeight];
        Parallel.For(0, CloudMapHeight, y =>
        {
            var lat = (0.5 - (y + 0.5) / CloudMapHeight) * Math.PI;
            for (var x = 0; x < CloudMapWidth; x++)
            {
                var lon = ((x + 0.5) / CloudMapWidth - 0.5) * 2 * Math.PI;
                // 3D 노이즈를 구 표면 좌표에서 뽑아 경도 이음매와 극 왜곡이 없게 한다.
                var px = Math.Cos(lat) * Math.Cos(lon);
                var py = Math.Sin(lat);
                var pz = Math.Cos(lat) * Math.Sin(lon);
                var n = Fbm(px * 2.6, py * 3.1, pz * 2.6);
                // 밀도 임계값으로 구름 덩어리와 빈 곳을 나눈다.
                map[y * CloudMapWidth + x] = (float)Math.Clamp((n - 0.57) * 3.2, 0.0, 0.80);
            }
        });
        return map;
    }

    private static double SampleCloud(float[] map, double u, double v)
    {
        var x = u * CloudMapWidth - 0.5;
        var y = Math.Clamp(v * CloudMapHeight - 0.5, 0.0, CloudMapHeight - 1.001);
        var x0 = (int)Math.Floor(x);
        var y0 = (int)Math.Floor(y);
        var fx = x - x0;
        var fy = y - y0;
        var xa = ((x0 % CloudMapWidth) + CloudMapWidth) % CloudMapWidth;
        var xb = (xa + 1) % CloudMapWidth;
        var yb = Math.Min(y0 + 1, CloudMapHeight - 1);
        var top = map[y0 * CloudMapWidth + xa] * (1 - fx) + map[y0 * CloudMapWidth + xb] * fx;
        var bottom = map[yb * CloudMapWidth + xa] * (1 - fx) + map[yb * CloudMapWidth + xb] * fx;
        return top * (1 - fy) + bottom * fy;
    }

    private static double Fbm(double x, double y, double z)
    {
        var sum = 0.0;
        var amplitude = 0.5;
        var frequency = 1.0;
        for (var octave = 0; octave < 5; octave++)
        {
            sum += amplitude * ValueNoise(x * frequency, y * frequency, z * frequency);
            amplitude *= 0.5;
            frequency *= 2.07;
        }

        return sum / 0.96875;
    }

    private static double ValueNoise(double x, double y, double z)
    {
        var xi = (int)Math.Floor(x);
        var yi = (int)Math.Floor(y);
        var zi = (int)Math.Floor(z);
        var fx = Fade(x - xi);
        var fy = Fade(y - yi);
        var fz = Fade(z - zi);
        double Corner(int dx, int dy, int dz) => Hash(xi + dx, yi + dy, zi + dz);
        var x00 = Lerp(Corner(0, 0, 0), Corner(1, 0, 0), fx);
        var x10 = Lerp(Corner(0, 1, 0), Corner(1, 1, 0), fx);
        var x01 = Lerp(Corner(0, 0, 1), Corner(1, 0, 1), fx);
        var x11 = Lerp(Corner(0, 1, 1), Corner(1, 1, 1), fx);
        return Lerp(Lerp(x00, x10, fy), Lerp(x01, x11, fy), fz);
    }

    private static double Fade(double t) => t * t * t * (t * (t * 6 - 15) + 10);

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;

    /// <summary>정수 격자점의 의사 난수(0~1). 결정적이므로 매번 같은 구름 지도가 나온다.</summary>
    private static double Hash(int x, int y, int z)
    {
        unchecked
        {
            var h = (uint)(x * 374761393 + y * 668265263 + z * 2147483647);
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / (double)0x1000000;
        }
    }

    /// <summary>텍스처 리소스가 정상적으로 읽히는지 확인한다(테스트와 진단용).</summary>
    internal static (int Width, int Height) TextureSize(PlanetKind kind)
    {
        var t = kind == PlanetKind.Earth ? EarthTexture.Value : MoonTexture.Value;
        return (t.Width, t.Height);
    }
}
