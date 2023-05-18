using System.Data;
using ezOverLay;
using swed32;
using winformtemplate;
using System.Runtime.InteropServices;
using System.Numerics;


namespace CSGO_ESP
{
    public partial class Form1 : Form
    {
        const int LocalPlayer = 0xDE997C;
        const int EntityList = 0x4DFEF0C;
        const int Viewmatrix = 0x4DEFD54;
        const int ClientState = 0x59F19C;
        const int CrosshairId = 0x11838;
        const int ForceAttack = 0x322CD48;

        const int VecOrigin = 0x138;
        const int VecViewOffset = 0x108;
        const int Team = 0xF4;
        const int Dormant = 0xED;
        const int Health = 0x100;
        const int MyWeapons = 0x2E08;
        const int ItemDefinitionIndex = 0x2FBA;
        const int FallbackPaintKit = 0x31D8;
        const int ItemIDHigh = 0x2FD0;
        const int ClientState_ViewAngles = 0x4D90;
        const int BoneMatrix = 0x26A8;

        readonly int[] BonesIndexes_model_1 = { 0, 7, 8, 11, 12, 13, 39, 40, 41, 67, 68, 74, 75 };
        readonly int[] BonesIndexes_model_2 = { 0, 7, 8, 11, 12, 13, 40, 41, 42, 73, 74, 82, 83 };
        readonly int[] BonesIndexes_model_3 = { 0, 7, 8, 11, 12, 13, 41, 42, 43, 71, 72, 78, 79 };

        int value = 0;
        
        Pen teamPen = new Pen(Color.FromArgb(0, 0, 255), 1);
        Pen enemyPen = new Pen(Color.FromArgb(255, 0, 0), 1);

        static Form f1;
        static Form2 f2;

        swed swed = new swed();
        ez ez = new ez();

        entity player = new entity();
        List<entity> entityList = new List<entity>();

        List<IDS.saved> saved = new List<IDS.saved>();

        IntPtr client, engine;


        [DllImport("user32.dll")]
        extern static short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;


        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true;

            f1 = this;
            f1.Text = "SB_ELO_ESP";

            f2 = new Form2();
            f2.Text = "Settings";
            f2.Show();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;
            swed.GetProcess("csgo");

            client = swed.GetModuleBase("client.dll");
            engine = swed.GetModuleBase("engine.dll");

            ez.SetInvi(this);
            ez.DoStuff("Counter-Strike: Global Offensive - Direct3D 9", this);

            Thread thread = new Thread(Main) { IsBackground = true };
            thread.Start();
        }


        void Main()
        {
            while (true)
            {
                UpdateLocalPlayer();

                UpdateEntites();

                UpdateAimBot();

                UpdateTriggerBot();

                Skinchanger();

                panel1.Refresh();

                Thread.Sleep(12);
            }
        }


        public void UpdateTriggerBot()
        {
            if (GetAsyncKeyState(f2.TriggerBot_key) < 0)
            {
                var buffer = swed.ReadPointer(client, LocalPlayer);
                var crosshairid = BitConverter.ToInt32(swed.ReadBytes(buffer, CrosshairId, 4), 0);
                var myteam = BitConverter.ToInt32(swed.ReadBytes(buffer, Team, 4), 0);

                var enemy = swed.ReadPointer(client, EntityList + (crosshairid - 1) * 0x10);
                var enemyTeam = BitConverter.ToInt32(swed.ReadBytes(enemy, Team, 4), 0);
                var enemyHealth = BitConverter.ToInt32(swed.ReadBytes(enemy, Health, 4), 0);

                if (myteam != enemyTeam && enemyHealth >= 1)
                {
                    Shoot();
                }
            }
        }

        public void Shoot()
        {
            swed.WriteBytes(client, ForceAttack, BitConverter.GetBytes(5));
            Thread.Sleep(1);
            swed.WriteBytes(client, ForceAttack, BitConverter.GetBytes(4));
        }


        public void UpdateAimBot()
        {
            double angle = f2.aimBotMaxAngle;
            double this_angle;
            entity target = null;
            if (f2.enable_AimBot)
            {
                List<entity> aimBotEntityList = new List<entity>(entityList.OrderBy(o => o.mag).ToList());

                if (GetAsyncKeyState(f2.AimBot_key) < 0 && aimBotEntityList.Count > 0)
                {
                    foreach (var ent in aimBotEntityList.ToList())
                    {
                        if (ent.team == player.team)
                        {
                            aimBotEntityList.Remove(ent);
                        }
                    }
                    if (aimBotEntityList.Count > 0)
                        foreach (var enemy in aimBotEntityList)
                        {
                            this_angle = GetDeltaAngle(enemy);
                            if (this_angle < angle)
                            {
                                angle = this_angle;
                                target = enemy;
                            }
                        }
                        if (target != null)
                        {
                            Aim(target);
                            Shoot();
                    }
                }
            }
        }


        public double GetDeltaAngle(entity ent)
        {
            float deltaX = ent.head.X - player.feet.X;
            float deltaY = ent.head.Y - player.feet.Y;
            float X = (float)(Math.Atan2(deltaY, deltaX) * 180 / Math.PI);
            float deltaZ = ent.head.Z - player.feet.Z;
            double dist = Math.Sqrt(Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2));
            float Y = -(float)(Math.Atan2(deltaZ, dist) * 180 / Math.PI);

            var buffer = swed.ReadPointer(engine, ClientState);
            float playerAngleX = BitConverter.ToSingle(swed.ReadBytes(buffer, ClientState_ViewAngles + 0x4, 4), 0);
            float playerAngleY = BitConverter.ToSingle(swed.ReadBytes(buffer, ClientState_ViewAngles, 4), 0);

            if (playerAngleX < 0)
            {
                playerAngleX = 360 + playerAngleX;
            }
            if (X < 0)
            {
                X = 360 + X;
            }

            playerAngleY = 90 - playerAngleY;
            Y = 90 - Y;

            double angleX = Math.Abs(X - playerAngleX);
            if (angleX > 180)
            {
                angleX= 360 - angleX;
            }

            double angleY = Math.Abs(Y - playerAngleY);
            return Math.Sqrt(angleX * angleX + angleY * angleY);
        }

        public void Aim(entity ent)
        {
            float deltaX = ent.head.X - player.feet.X;
            float deltaY = ent.head.Y - player.feet.Y;
            float X = (float)(Math.Atan2(deltaY, deltaX) * 180 / Math.PI);

            float deltaZ = ent.head.Z - player.feet.Z;

            double dist = Math.Sqrt(Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2));

            float Y = -(float)(Math.Atan2(deltaZ, dist) * 180 / Math.PI);

            var buffer = swed.ReadPointer(engine, ClientState);

            swed.WriteBytes(buffer, ClientState_ViewAngles, BitConverter.GetBytes(Y));
            swed.WriteBytes(buffer, ClientState_ViewAngles + 0x4, BitConverter.GetBytes(X));
        }

        public void SkinchangerButton(int weaponid, int skinid, float wear)
        {
            var newskin = new IDS.saved { weaponid = weaponid, skinid = skinid, wear = wear };


            foreach (var item in saved.ToList())
            {
                if (item.weaponid == newskin.weaponid)
                    saved.Remove(item);
            }

            saved.Add(newskin);

        }


        void Skinchanger()
        {
            if (f2.enable_Skin_changer)
            {
                var buffer = swed.ReadPointer(client, LocalPlayer);

                for (int i = 0; i < 3; i++)
                {
                    var currentweapon = BitConverter.ToInt32(swed.ReadBytes(buffer, MyWeapons + i * 0x4, 4), 0) & 0xfff;

                    var weaponpointer = swed.ReadPointer(client, EntityList + (currentweapon - 1) * 0x10);

                    var weaponid = BitConverter.ToInt16(swed.ReadBytes(weaponpointer, ItemDefinitionIndex, 2), 0);


                    var setting = GetSkin(weaponid);

                    if (setting != null)
                    {
                        ApplySkin(weaponpointer, setting);
                    }
                }
            }
        }


        void ApplySkin(IntPtr entpointer, IDS.saved skinsetting)
        {
            var currentskin = BitConverter.ToInt32(swed.ReadBytes(entpointer, FallbackPaintKit, 4), 0);

            if (currentskin != skinsetting.skinid)
            {
                swed.WriteBytes(entpointer, ItemIDHigh, BitConverter.GetBytes(-1));
                swed.WriteBytes(entpointer, FallbackPaintKit, BitConverter.GetBytes(skinsetting.skinid));
                var clientstate = swed.ReadPointer(engine, ClientState);
                swed.WriteBytes(clientstate, 0x174, BitConverter.GetBytes(-1));
            }
        }


        IDS.saved? GetSkin(int currntid)
        {
            foreach (var skin in saved)
            {
                if (skin.weaponid == currntid)
                {
                    return skin;
                }
            }

            return null;
        }


        void UpdateLocalPlayer()
        {
            var buffer = swed.ReadPointer(client, LocalPlayer);
            var coords = swed.ReadBytes(buffer, VecOrigin, 12);

            player.feet.X = BitConverter.ToSingle(coords, 0);
            player.feet.Y = BitConverter.ToSingle(coords, 4);
            player.feet.Z = BitConverter.ToSingle(coords, 8);


            player.team = BitConverter.ToInt32(swed.ReadBytes(buffer, Team, 4), 0);
            player.feet.Z += BitConverter.ToSingle(swed.ReadBytes(buffer, VecViewOffset + 0x8, 4), 0);
        }


        void UpdateEntites()
        {
            entityList.Clear();

            for (int i = 0; i < 32; i++)
            {
                var buffer = swed.ReadPointer(client, EntityList + i * 0x10);

                var tm = BitConverter.ToInt32(swed.ReadBytes(buffer, Team, 4), 0);
                var dorm = BitConverter.ToInt32(swed.ReadBytes(buffer, Dormant, 4), 0);
                var hp = BitConverter.ToInt32(swed.ReadBytes(buffer, Health, 4), 0);

                if (hp < 1 || dorm != 0 || buffer == swed.ReadPointer(client, LocalPlayer))
                {
                    continue;
                }

                var coords = swed.ReadBytes(buffer, VecOrigin, 12);

                var ent = new entity
                {
                    head = GetHead(buffer),
                    x = BitConverter.ToSingle(coords, 0),
                    y = BitConverter.ToSingle(coords, 4),
                    z = BitConverter.ToSingle(coords, 8),
                    team = tm,
                    health = hp
                };

                viewmatrix vmtrx = ReadMatrix();
                ent.bot = WorldToScreen(vmtrx, ent.x, ent.y, ent.z, Width, Height);
                ent.top = WorldToScreen(vmtrx, ent.x, ent.y, ent.z + 58, Width, Height);

                ent.mag = GetMag(player.feet, ent.head);

                if (ent.team == 2)
                {
                    ent.boneMatrix = GetBoneMatrix(buffer, BonesIndexes_model_1);
                }
                else
                {
                    ent.boneMatrix = GetBoneMatrix(buffer, BonesIndexes_model_2);
                }
                
                
                entityList.Add(ent);
            }
        }


        float GetMag(Vector3 player, Vector3 enemy)
        {
            return (float)(Math.Sqrt(
                Math.Pow(enemy.X - player.X, 2) +
                Math.Pow(enemy.Y - player.Y, 2) +
                Math.Pow(enemy.Z - player.Z, 2)
            ));
        }

        Vector3 GetHead(IntPtr buffer)
        {
            var bones = swed.ReadPointer(buffer, BoneMatrix);
            var bone = swed.ReadBytes(bones, 0x30 * 8, 0x30);

            return new Vector3
            {
                X = BitConverter.ToSingle(bone, 0xC),
                Y = BitConverter.ToSingle(bone, 0x1C),
                Z = BitConverter.ToSingle(bone, 0x2C)
            };
        }


        List<Vector3> GetBoneMatrix(IntPtr buffer, int[] BonesIndexes)
        {
            List<Vector3> matrix = new List<Vector3>();
            var bones = swed.ReadPointer(buffer, BoneMatrix);

            foreach (int index in BonesIndexes)
            {
                var bone = swed.ReadBytes(bones, 0x30 * index, 0x30);
                matrix.Add(new Vector3
                {
                    X = BitConverter.ToSingle(bone, 0xC),
                    Y = BitConverter.ToSingle(bone, 0x1C),
                    Z = BitConverter.ToSingle(bone, 0x2C)
                });
            }

            return matrix;
        }


        viewmatrix ReadMatrix()
        {
            var matrix = new viewmatrix();

            var buffer = new byte[16 * 4];

            buffer = swed.ReadBytes(client, Viewmatrix, buffer.Length);

            matrix.m11 = BitConverter.ToSingle(buffer, 0 * 4);
            matrix.m12 = BitConverter.ToSingle(buffer, 1 * 4);
            matrix.m13 = BitConverter.ToSingle(buffer, 2 * 4);
            matrix.m14 = BitConverter.ToSingle(buffer, 3 * 4);

            matrix.m21 = BitConverter.ToSingle(buffer, 4 * 4);
            matrix.m22 = BitConverter.ToSingle(buffer, 5 * 4);
            matrix.m23 = BitConverter.ToSingle(buffer, 6 * 4);
            matrix.m24 = BitConverter.ToSingle(buffer, 7 * 4);

            matrix.m31 = BitConverter.ToSingle(buffer, 8 * 4);
            matrix.m32 = BitConverter.ToSingle(buffer, 9 * 4);
            matrix.m33 = BitConverter.ToSingle(buffer, 10 * 4);
            matrix.m34 = BitConverter.ToSingle(buffer, 11 * 4);

            matrix.m41 = BitConverter.ToSingle(buffer, 12 * 4);
            matrix.m42 = BitConverter.ToSingle(buffer, 13 * 4);
            matrix.m43 = BitConverter.ToSingle(buffer, 14 * 4);
            matrix.m44 = BitConverter.ToSingle(buffer, 15 * 4);

            return matrix;
        }


        Point WorldToScreen(viewmatrix mtx, float x, float y, float z, int width, int height)
        {
            var twoD = new Point();

            float screenW = (mtx.m41 * x) + (mtx.m42 * y) + (mtx.m43 * z) + mtx.m44;

            if (screenW > 0.001f)
            {
                float screenX = (mtx.m11 * x) + (mtx.m12 * y) + (mtx.m13 * z) + mtx.m14;
                float screenY = (mtx.m21 * x) + (mtx.m22 * y) + (mtx.m23 * z) + mtx.m24;

                float camX = width / 2f;
                float camY = height / 2f;

                float X = camX + (camX * screenX / screenW);
                float Y = camY - (camY * screenY / screenW);

                twoD.X = (int)X;
                twoD.Y = (int)Y;

                return twoD;
            }

            else
            {
                return new Point(-99, -99);
            }
        }

        public void DrawHPValue(Graphics g, entity ent)
        {
            int hp = ent.health;

            String String = hp.ToString();

            SolidBrush Brush = new SolidBrush(Color.FromArgb((int)(255 * MathF.Pow((float)(1 - hp / 100.0f), 0.4f)), (int)(255 * MathF.Pow((float)(hp / 100.0f), 0.4f)), 0));

            Font drawFont = new Font("Arial", (int)(ent.bot.Y - ent.top.Y) / 8);

            int x = ent.bot.X - (int)(ent.bot.Y - ent.top.Y) / 3;
            int y = ent.top.Y - (int)(ent.bot.Y - ent.top.Y) / 5;

            g.DrawString(String, drawFont, Brush, x, y);
        }

        public void DrawHPBar(Graphics g, entity ent)
        {
            int hp = ent.health;
            Pen hp_pen = new Pen(Color.FromArgb((int)(255 * MathF.Pow((float)(1 - hp / 100.0f), 0.4f)), (int)(255 * MathF.Pow((float)(hp / 100.0f), 0.4f)), 0), 10);
            Point bottom = new Point(ent.rect().Left - 10, ent.rect().Bottom + 1);
            Point top = new Point(ent.rect().Left - 10, ent.rect().Top + (int)((double)(ent.rect().Bottom - ent.rect().Top) * (1 - (double)hp / 100)));
            g.DrawLine(hp_pen, top, bottom);
        }

        public void DrawSkeleton(Graphics g, entity ent)
        {
            Pen pen = new Pen(Color.White);
            Point[] bonePoints = new Point[13];
            viewmatrix vmtrx = ReadMatrix();

            for (int i = 0; i < 13; i++)
            {
                bonePoints[i] = WorldToScreen(vmtrx, ent.boneMatrix[i].X, ent.boneMatrix[i].Y, ent.boneMatrix[i].Z, Width, Height);
            }

            g.DrawLine(pen, bonePoints[0], bonePoints[1]);
            // g.DrawLine(pen, bonePoints[1], bonePoints[2]);
            g.DrawLine(pen, bonePoints[1], bonePoints[3]);
            g.DrawLine(pen, bonePoints[3], bonePoints[4]);
            g.DrawLine(pen, bonePoints[4], bonePoints[5]);
            g.DrawLine(pen, bonePoints[1], bonePoints[6]);
            g.DrawLine(pen, bonePoints[6], bonePoints[7]);
            g.DrawLine(pen, bonePoints[7], bonePoints[8]);
            g.DrawLine(pen, bonePoints[0], bonePoints[9]);
            g.DrawLine(pen, bonePoints[9], bonePoints[10]);
            g.DrawLine(pen, bonePoints[0], bonePoints[11]);
            g.DrawLine(pen, bonePoints[11], bonePoints[12]);

            int r = (int)Math.Sqrt(Math.Pow(bonePoints[2].X - bonePoints[1].X, 2) + Math.Pow(bonePoints[2].Y - bonePoints[1].Y, 2));
            g.DrawEllipse(pen, bonePoints[2].X - r, bonePoints[2].Y - r, 2*r, 2*r);
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            enemyPen.Width = f2.enemy_line_width;
            teamPen.Width = f2.team_line_width;
            Font drawFont = new Font("Arial", 30);

            var g = e.Graphics;

            try
            {
                foreach (var ent in entityList)
                {
                    DrawSkeleton(g, ent);

                    if (f2.enable_ESP)
                    {
                        if (ent.team == player.team)
                        {
                            g.DrawRectangle(teamPen, ent.rect());
                            g.DrawLine(teamPen, Width / 2, Height, ent.bot.X, ent.bot.Y);

                        }

                        else if (ent.team != player.team)
                        {
                            g.DrawRectangle(enemyPen, ent.rect());
                            g.DrawLine(enemyPen, Width / 2, Height, ent.bot.X, ent.bot.Y);
                        }
                    }

                    

                    if (f2.enable_HP)
                    {
                        DrawHPBar(g, ent);
                        //DrawHPValue(g, ent);
                    }
                }
            }
            catch { }

            if (f2.enable_AimBot == true)
            {
                int r = (int)((Width / 2) * Math.Sin(f2.aimBotMaxAngle * (Math.PI / 108)));
                g.DrawEllipse(new Pen(Color.White), (Width - r) / 2, (Height - r) / 2, r, r);
            }

            g.DrawString(value.ToString(), drawFont, new SolidBrush(Color.White), 10, 1000);
        }
    }
}
