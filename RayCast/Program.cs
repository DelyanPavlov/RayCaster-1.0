using Raylib_cs;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace RayCast;

#region structs


struct Vector
{
    public Vector2 pos;
    public Vector2 dir;

    public Vector(Vector2 pos, Vector2 dir)
    {
        this.pos = pos;
        this.dir = dir;
    }
}

struct Point
{
    public int X;
    public int Y;

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static Point operator *(Point a, Point b)
    {
        return new Point(a.X * b.X, a.Y * b.Y);
    }
    public static Point operator +(Point a, Point b)
    {
        return new Point(a.X + b.X, a.Y + b.Y);
    }
    public static Point operator -(Point a, Vector2 b)
    {
        return new Point(a.X - (int)b.X, a.Y - (int)b.Y);
    }
    public static Point operator +(Point a, Vector2 b)
    {
        return new Point(a.X + (int)MathF.Round(b.X), a.Y + (int)MathF.Round(b.Y));
    }
}

struct Line
{
    public Point p1;
    public Point p2;

    public Line(Point p1, Point p2)
    {
        this.p1 = p1;
        this.p2 = p2;
    }

    public static Line operator *(Line a, Line b)
    {
        return new Line(a.p1 * b.p1, a.p2 * b.p2);
    }
}
#endregion


static class Program
{
    public static List<float> Ucord = new List<float>();
    [System.STAThread]
    public static void Main()
    {
        Vector2 move = new Vector2(0, 0);
        Vector2 player = new Vector2(377, 772);
        Line[] walls = {
    // Outer walls
    new ( new Point(50, 50),   new Point(50, 590)),
    new ( new Point(50, 590),  new Point(590, 590)),
    new ( new Point(590, 590), new Point(590, 50)),
    new ( new Point(590, 50),  new Point(50, 50)),

    // Top-left room
    new ( new Point(50, 200),  new Point(175, 200)),
    new ( new Point(175, 50),  new Point(175, 200)),

    // Top-right room
    new ( new Point(450, 50),  new Point(450, 200)),
    new ( new Point(450, 200), new Point(590, 200)),

    // Center divider with gap
    new ( new Point(250, 150), new Point(250, 290)),
    new ( new Point(390, 150), new Point(390, 290)),
    new ( new Point(250, 150), new Point(390, 150)),

    // Bottom corridor walls
    new ( new Point(50, 400),  new Point(200, 400)),
    new ( new Point(200, 400), new Point(200, 320)),
    new ( new Point(440, 400), new Point(590, 400)),
    new ( new Point(440, 400), new Point(440, 320)),

    // Center bottom box
    new ( new Point(270, 375), new Point(370, 375)),
    new ( new Point(270, 375), new Point(270, 475)),
    new ( new Point(370, 375), new Point(370, 475)),
    new ( new Point(270, 475), new Point(370, 475)),

    // Bottom-left alcove
    new ( new Point(50, 500),  new Point(150, 500)),
    new ( new Point(150, 500), new Point(150, 590)),

    // Bottom-right alcove
    new ( new Point(590, 500), new Point(490, 500)),
    new ( new Point(490, 500), new Point(490, 590)),

    // Diagonal
    new ( new Point(200, 225), new Point(250, 290)),
};
        int textH, textW, screenW = 1280, screenH = 720, moveSpeed = 9, rotateSpeed = 11, chanels;
        Vector[] rays = new Vector[screenW];
        float length = 0f, wallSize = 30, fov = 60f, currHead = -90f, DT = 0f;
        float[] closest = new float[rays.Length], closestT = new float[rays.Length];
        byte[] texture = LoadTGA(@"C:\Users\Delyan\Downloads\wall.tga", out textH, out textW, out chanels);
        bool drawData = false;

        Raylib.InitWindow(screenW, screenH, "Ray caster");
        Raylib.SetTargetFPS(240);


        for (int i = 0; i < rays.Length; i++)
        {
            rays[i] = new Vector(player, angle(0));
        }

        while (!Raylib.WindowShouldClose())
        {
            DT = Raylib.GetFrameTime() * 10;
            for (int i = 0; i < rays.Length; i++)
            {
                closest[i] = int.MaxValue;
            }
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);

            if (Raylib.IsKeyPressed(KeyboardKey.F3))
            {
                drawData = !drawData;
            }

            if (Raylib.IsKeyDown(KeyboardKey.Right))
            {
                currHead += 1 * DT * rotateSpeed;
            }
            else if (Raylib.IsKeyDown(KeyboardKey.Left))
            {
                currHead -= 1 * DT * rotateSpeed;
            }

            move = new Vector2(0, 0);
            if (Raylib.IsKeyDown(KeyboardKey.A)) move += angle(currHead - 90);
            if (Raylib.IsKeyDown(KeyboardKey.D)) move += angle(currHead + 90);
            if (Raylib.IsKeyDown(KeyboardKey.W)) move += angle(currHead);
            if (Raylib.IsKeyDown(KeyboardKey.S)) move -= angle(currHead);
            Vector2.Normalize(move);
            player += move * 2 * DT * new Vector2(moveSpeed, moveSpeed) * 0.75f;


            Vector2 temp = new Vector2();

            for (int j = 0; j < rays.Length; j++)
            {
                float rayAngle = j * (fov / rays.Length) + currHead - fov / 2;
                rays[j] = new Vector(player, angle(rayAngle));

                for (int i = 0; i < walls.Length; i++)
                {
                    temp = RayLineInterection(rays[j], walls[i]);
                    length = temp[0];

                    float offsetAngle = (rayAngle - currHead) * (MathF.PI / 180f);
                    float correctedLength = length * MathF.Cos(offsetAngle);

                    if (correctedLength < closest[j])
                    {
                        closest[j] = correctedLength;

                        float wallLength = Vector2.Distance(
                            new Vector2(walls[i].p1.X, walls[i].p1.Y),
                            new Vector2(walls[i].p2.X, walls[i].p2.Y)
                        );
                        closestT[j] = (temp[1] * wallLength / textW) % 1.0f;
                    }
                }
                // remove the offsetAngle and cos correction that was after the loop
            }
            
            Color wallColor;
            float projectionDist = (rays.Length / 2f) / MathF.Tan((fov / 2f) * MathF.PI / 180f);

            for (int i = 0; i < rays.Length; i++)
            {
                Random rnd = new Random();
                float wallHeight = (wallSize * projectionDist) / closest[i];
                int individualHeight = (int)MathF.Ceiling(MathF.Max(wallHeight / textH, 1f));
                int currScreenH = individualHeight * textH;
                int screenMidY = screenH / 2;
                int top = (int)(screenMidY - wallHeight / 2);
                int bottom = (int)(screenMidY + wallHeight / 2);
                int texCol = (int)(closestT[i] * textW);
                int tempSize = 0, ind = 0, offset = top;
                texCol = Math.Clamp(texCol, 0, textW - 1);

                int[] heights = new int[textH];
                Array.Fill(heights, individualHeight);

                if (heights[0] != 1)
                {
                    for (int j = 0; j < heights.Length; j++)
                    {
                        tempSize += heights[j];
                    }

                    while (tempSize > wallHeight)
                    {
                        heights[ind]--;
                        tempSize--;
                        ind++;//= rnd.Next(0, heights.Length - 1);
                    }
                }



                for (int j = 0; j < textH; j++)
                {
                    int colorInd = Math.Max((j * textW + texCol) * chanels, 0);
                    wallColor = new Color(texture[colorInd], texture[colorInd + 1], texture[colorInd + 2]);
                    Raylib.DrawLine(i, offset, i, offset + heights[j], wallColor);
                    offset += heights[j];
                }
            }

            if (drawData)
            {
                Raylib.DrawFPS(10, 10);
                Raylib.DrawText($"{MathF.Round(DT * 10, 3)} ms", 10, 30, 20, Color.DarkGreen);
            }

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }


    public static Vector2 RayLineInterection(Vector ray, Line wall)
    {
        float x1 = wall.p1.X;
        float y1 = wall.p1.Y;
        float x2 = wall.p2.X;
        float y2 = wall.p2.Y;

        float x3 = ray.pos.X;
        float y3 = ray.pos.Y;
        float x4 = ray.pos.X + ray.dir.X;
        float y4 = ray.pos.Y + ray.dir.Y;

        float den = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);

        if (den == 0)
        {
            return new Vector2(int.MaxValue, 0f);
        }

        float t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / den;
        float u = -((x1 - x2) * (y1 - y3) - (y1 - y2) * (x1 - x3)) / den;

        if (t >= 0f && t <= 1f && u > 0) return new Vector2(u, t);

        return new Vector2(int.MaxValue, int.MaxValue);
    }

    public static Vector2 angle(float angle)
    {
        float angleRad = (float)(angle * (Math.PI / 180));
        return new Vector2(MathF.Cos(angleRad), MathF.Sin(angleRad));
    }

    public static byte[] LoadTGA(string filePath, out int width, out int height, out int channels)
    {
        using (BinaryReader br = new BinaryReader(File.OpenRead(filePath)))
        {
            byte idLength = br.ReadByte();
            byte colorMapType = br.ReadByte();
            byte imageType = br.ReadByte();

            if (imageType != 2)
                throw new Exception("Only uncompressed RGB(A) TGA (type 2) is supported.");

            br.BaseStream.Seek(5, SeekOrigin.Current);

            br.BaseStream.Seek(4, SeekOrigin.Current);

            width = br.ReadInt16();
            height = br.ReadInt16();
            byte bpp = br.ReadByte();
            byte descriptor = br.ReadByte();

            channels = bpp / 8;
            if (channels != 3 && channels != 4)
                throw new Exception("Only 24-bit or 32-bit TGA supported.");

            if (idLength > 0)
                br.BaseStream.Seek(idLength, SeekOrigin.Current);

            int pixelCount = width * height;
            byte[] data = br.ReadBytes(pixelCount * channels);

            for (int i = 0; i < data.Length; i += channels)
            {
                byte temp = data[i];
                data[i] = data[i + 2];
                data[i + 2] = temp;
            }

            bool originTop = (descriptor & 0x20) != 0;
            if (!originTop)
            {
                int stride = width * channels;
                byte[] flipped = new byte[data.Length];

                for (int y = 0; y < height; y++)
                {
                    Buffer.BlockCopy(
                        data, y * stride,
                        flipped, (height - 1 - y) * stride,
                        stride
                    );
                }

                data = flipped;
            }

            return data;
        }
    }


}