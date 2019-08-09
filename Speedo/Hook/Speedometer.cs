using SharpDX;
using SharpDX.Direct3D9;
using System;

namespace Speedo.Hook
{
    internal class Speedometer : IDisposable
    {
        private Vector3 vector = new Vector3(0.0f, 0.0f, 0.0f);
        private readonly Sprite[] _numbers = new Sprite[3];
        private float MaxSpeed = 275f;
        private float MaxAngle = 243f;
        private Sprite _dial;
        private Texture carTexture;
        private readonly Texture boatTexture;
        private readonly Texture planeTexture;
        private Texture needleTexture;
        private readonly Texture numberTexture;
        private readonly float _scale;
        private readonly float _posX;
        private readonly float _posY;

        public Speedometer(Device device, float scale, int x, int y)
        {
            _scale = scale;
            _dial = new Sprite(device);
            carTexture = Texture.FromFile(device, AppContext.BaseDirectory + "\\Resources\\speedocar.png");
            boatTexture = Texture.FromFile(device, AppContext.BaseDirectory + "\\Resources\\speedoboat.png");
            planeTexture = Texture.FromFile(device, AppContext.BaseDirectory + "\\Resources\\speedoplane.png");
            needleTexture = Texture.FromFile(device, AppContext.BaseDirectory + "\\Resources\\needle.png");
            numberTexture = Texture.FromFile(device, AppContext.BaseDirectory + "\\Resources\\font.png");
            FontLookup.ReadXML();
            _posX = x;
            _posY = y;
        }

        public void Draw(float speed, Speed.CurrentModeEnum mode)
        {
            if (speed < 0.0)
            {
                speed = -speed;
            }

            _dial.Begin(SpriteFlags.AlphaBlend);
            _dial.Transform = MatrixTransformSpeedo();
            DrawDial(mode);
            RotateNeedle(speed);
            _dial.Draw(needleTexture, Color.White * 0.9f, new Rectangle?(), new Vector3?(), new Vector3?());
            DrawSpeed(speed, new Vector2(_posX + 161f * _scale, _posY + 214f * _scale));
            _dial.End();
        }

        private void DrawSpeed(float speed, Vector2 startPos)
        {
            startPos.X += (float)(21.0 * _scale * 3.0);
            char[] charArray = string.Format("{0:0}", speed).ToCharArray();
            Array.Reverse(charArray);
            foreach (char letter in new string(charArray))
            {
                FontLocation letterLocation = FontLookup.FindLetterLocation(letter);
                _dial.Transform = Matrix.Transformation2D(new Vector2(0.0f, 0.0f), 0.0f, new Vector2(_scale, _scale), new Vector2(0.0f, 0.0f), 0.0f, startPos);
                Rectangle rectangle = new Rectangle(letterLocation.x, letterLocation.y, letterLocation.width, letterLocation.height);
                _dial.Draw(numberTexture, Color.White * 0.9f, new Rectangle?(rectangle), new Vector3?(), new Vector3?());
                startPos.X -= 21f * _scale;
            }
        }

        public void DrawDial(Speed.CurrentModeEnum mode)
        {
            switch (mode)
            {
                case Speed.CurrentModeEnum.Car:
                    MaxSpeed = 275f;
                    MaxAngle = 243f;
                    _dial.Draw(carTexture, Color.White * 0.9f, new Rectangle?(), new Vector3?(), new Vector3?());
                    break;
                case Speed.CurrentModeEnum.Boat:
                    MaxSpeed = 287f;
                    MaxAngle = 243f;
                    _dial.Draw(boatTexture, Color.White * 0.9f, new Rectangle?(), new Vector3?(), new Vector3?());
                    break;
                case Speed.CurrentModeEnum.Plane:
                    MaxSpeed = 355f;
                    MaxAngle = 243f;
                    _dial.Draw(planeTexture, Color.White * 0.9f, new Rectangle?(), new Vector3?(), new Vector3?());
                    break;
            }
        }

        public void RotateNeedle(float speed)
        {
            if (speed > MaxSpeed * 1.5)
            {
                speed = MaxSpeed * 1.5f;
            }

            float x = 16f * _scale;
            float y = 16f * _scale;
            float rotation = (float)(MaxAngle / MaxSpeed * (double)speed * (Math.PI / 180.0));
            _dial.Transform = Matrix.Transformation2D(new Vector2(0.0f, 0.0f), 0.0f, new Vector2(_scale, _scale), new Vector2(x, y), rotation, new Vector2(_posX + 128f * _scale - x, _posY + 128f * _scale - y));
        }

        private Matrix MatrixTransformSpeedo()
        {
            return Matrix.Transformation2D(new Vector2(0.0f, 0.0f), 0.0f, new Vector2(_scale, _scale), new Vector2(128f * _scale, 128f * _scale), 0.0f, new Vector2(_posX, _posY));
        }

        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            carTexture.Dispose();
            boatTexture.Dispose();
            planeTexture.Dispose();
            needleTexture.Dispose();
            numberTexture.Dispose();
            _dial.Dispose();
        }
    }
}
