using System;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using d3d = Microsoft.DirectX.Direct3D;

using WiccanRede.Graphics.Scene;

namespace WiccanRede.Graphics
{
    /// <summary>
    /// Trida zajistujici obsluhu kamery
    /// </summary>
    class Camera : ISceneCamera
    {
        private static Camera instance = null;
        private Device dev = null;

        private Matrix matrixView = Matrix.Identity;
        private Matrix matrixProjection = Matrix.Identity;
        private Matrix matrixViewProjection = Matrix.Identity;
        private ClipVolume clipVolume;

        private Vector3 position;
        private Vector3 lookAt;
        private Vector3 up = new Vector3(0, 1, 0);
        private float farDistance = 10000f;

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="device">Objekt zarizeni, ke kteremu se kamera vaze</param>
        private Camera(Device device)
        {
            this.position = new Vector3(-3000, 1065, -3000);
            this.lookAt = new Vector3(-3000, 1065, -3010);
            this.dev = device;

            UpdateLook();
        }

        /// <summary>
        /// Funkce pro obnoveni obrazu
        /// </summary>
        public void UpdateLook()
        {
            if (GraphicCore.GetInitializator().IsDeviceLost())
                return;

            matrixProjection = Matrix.PerspectiveFovLH((float)(Math.PI / 4f), (float)dev.PresentationParameters.DeviceWindow.Width / (float)dev.PresentationParameters.DeviceWindow.Height, 5f, farDistance);
            matrixView = Matrix.LookAtLH(position, lookAt, up);

            matrixViewProjection = matrixView * matrixProjection;

            clipVolume = ComputeClipVolume();
        }

        /// <summary>
        /// Funkce nastavi novy objekt zarizeni tride Camera 
        /// </summary>
        /// <param name="device">Objekt zarizeni</param>
        public static void SetCameraDevice(Device device)
        {
            instance = new Camera(device);
        }

        /// <summary>
        /// Funkce vrati instanci tridy Camera
        /// </summary>
        /// <returns>Objekt tridy Camera</returns>
        public static Camera GetCameraInstance()
        {
            return instance;
        }

        /// <summary>
        /// Funkce vrati nasobek View a Projection matice
        /// </summary>
        /// <returns>Funkce vrati nasobek View a Projection matice</returns>
        public Matrix GetMatrixViewProjection()
        {
            return matrixViewProjection;
        }

        /// <summary>
        /// Vrati projekcni matici
        /// </summary>
        /// <returns>Vraci projekcni matici</returns>
        public Matrix GetMatrixProjection()
        {
            return matrixProjection;
        }

        /// <summary>
        /// Funkce vrati invertovanou a transponovanou View matici
        /// </summary>
        /// <returns>Funkce vrati invertovanou a transponovanou View matici</returns>
        public Matrix GetViewITMatrix()
        {
            return Matrix.TransposeMatrix(Matrix.Invert(matrixView));
        }

        /// <summary>
        /// Funkce vrati View matici
        /// </summary>
        /// <returns>Funkce vrati View matici</returns>
        public Matrix GetMatrixView()
        {
            return matrixView;
        }

        /// <summary>
        /// Nastavi maximalni viditelnou vzdalenost
        /// </summary>
        /// <param name="farDistance">Maximalni vzdalenost od kamery, ktera bude viditelna</param>
        public void SetFarDistance(float farDistance)
        {
            this.farDistance = farDistance;
        }

        #region Get Camera Vectors

        /// <summary>
        /// Vrati vektor pozice
        /// </summary>
        /// <returns>Vrati vektor pozice</returns>
        public Vector3 GetVector3Position()
        {
            return position;
        }

        /// <summary>
        /// Vrati lookAt vektor
        /// </summary>
        /// <returns>Vrati lookAt vektor</returns>
        public Vector3 GetVector3LookAt()
        {
            return lookAt;
        }

        /// <summary>
        /// Vrati up vektor
        /// </summary>
        /// <returns>Vrati up vektor</returns>
        public Vector3 GetVector3Up()
        {
            return up;
        }
        #endregion

        #region Set Camera Vectors

        /// <summary>
        /// Nastavi pozici kamery
        /// </summary>
        /// <param name="position">Vektor s pozici kamery</param>
        public void SetCameraPositionVector(Vector3 position)
        {
            this.position = position;
            UpdateLook();
        }

        /// <summary>
        /// Nastavi bod, kam kamera kouka
        /// </summary>
        /// <param name="lookat">Vektor, se souradnicema, kam kamera kouka</param>
        public void SetCameraLookAtVector(Vector3 lookat)
        {
            this.lookAt = lookat;
            UpdateLook();
        }

        /// <summary>
        /// Nastavi up vektor kamery
        /// </summary>
        /// <param name="up">Nastavi up vektor kamery</param>
        public void SetCameraUpVector(Vector3 up)
        {
            this.up = up;
            UpdateLook();
        }

        #endregion

        /// <summary>
        /// Funkce pocitaci orezavaci roviny
        /// </summary>
        /// <returns>Vraci strukturu s popisem orezavacich rovin</returns>
        private ClipVolume ComputeClipVolume()
        {
            ClipVolume clvol = new ClipVolume();
            Matrix multiply = Matrix.Multiply(matrixView, matrixProjection);

            //Near plane
            clvol.pNear.A = multiply.M14 + multiply.M13;
            clvol.pNear.B = multiply.M24 + multiply.M23;
            clvol.pNear.C = multiply.M34 + multiply.M33;
            clvol.pNear.D = multiply.M44 + multiply.M43;
            clvol.pNear.Normalize();

            //Far plane
            clvol.pFar.A = multiply.M14 - multiply.M13;
            clvol.pFar.B = multiply.M24 - multiply.M23;
            clvol.pFar.C = multiply.M34 - multiply.M33;
            clvol.pFar.D = multiply.M44 - multiply.M43;
            clvol.pFar.Normalize();

            //Left plane
            clvol.pLeft.A = multiply.M14 + multiply.M11;
            clvol.pLeft.B = multiply.M24 + multiply.M21;
            clvol.pLeft.C = multiply.M34 + multiply.M31;
            clvol.pLeft.D = multiply.M44 + multiply.M41;
            clvol.pLeft.Normalize();

            //Right plane
            clvol.pRight.A = multiply.M14 - multiply.M11;
            clvol.pRight.B = multiply.M24 - multiply.M21;
            clvol.pRight.C = multiply.M34 - multiply.M31;
            clvol.pRight.D = multiply.M44 - multiply.M41;
            clvol.pRight.Normalize();

            //Top plane
            clvol.pTop.A = multiply.M14 - multiply.M12;
            clvol.pTop.B = multiply.M24 - multiply.M22;
            clvol.pTop.C = multiply.M34 - multiply.M32;
            clvol.pTop.D = multiply.M44 - multiply.M42;
            clvol.pTop.Normalize();

            //Bottom plane
            clvol.pBottom.A = multiply.M14 + multiply.M12;
            clvol.pBottom.B = multiply.M24 + multiply.M22;
            clvol.pBottom.C = multiply.M34 + multiply.M32;
            clvol.pBottom.D = multiply.M44 + multiply.M42;
            clvol.pBottom.Normalize();

            return clvol;
        }

        /// <summary>
        /// Metoda vrati orezavaci roviny
        /// </summary>
        /// <returns>Metoda vrati orezavaci roviny</returns>
        public ClipVolume GetClipVolume()
        {
            return clipVolume;
        }

        /// <summary>
        /// Metoda vrati smer, kterym miri paprsek ve 3D prostoru
        /// </summary>
        /// <param name="position">2D souradnice, odkud chceme vystrelit paprsek</param>
        /// <returns>Smer paprsku ve 3D, ktery protina zadanej bod</returns>
        public Vector3 GetDirectionVector2(Vector2 position)
        {
            Vector3 screenPosFar = new Vector3(position.X, position.Y, 1);
            Vector3 screenPosNear = new Vector3(position.X, position.Y, 0);

            screenPosFar.Unproject(dev.Viewport, matrixProjection, matrixView, Matrix.Identity);
            screenPosNear.Unproject(dev.Viewport, matrixProjection, matrixView, Matrix.Identity);

            Vector3 dir = screenPosFar - screenPosNear;
            dir.Normalize();

            return dir;
        }

    }


    /// <summary>
    /// Trida obsluhuje pohyb hrace
    /// </summary>
    static class CameraDriver
    {
        private static float Speed = 0.2f;
        private static Vector3 HeightOfEye = new Vector3(0, 50f, 5);
        private static float Sensitivity = 8f;
        private static int time = 1;
        private static bool freeLook = true;
        private static bool invertedmouse = false;
        private static bool showPlayer = false;

        private static WiccanRede.Objects.Player player;
        private static Matrix originalPlayerWorld = Matrix.Identity;
        private static WiccanRede.AI.IWalkable terain = null;

        /// <summary>
        /// Vrati vysku oci
        /// </summary>
        /// <returns>Vrati vysku oci</returns>
        public static Vector3 GetHeightOfEye()
        {
            return CameraDriver.HeightOfEye;
        }

        /// <summary>
        /// Nastavi, zda bude ci nebude videt hrac a vse s nim spojene
        /// </summary>
        /// <param name="enable">True - hrac bude videt, jinak false</param>
        public static void SetShowPlayer(bool enable)
        {
            showPlayer = enable;

            SceneManager sceneManager = GraphicCore.GetCurrentSceneManager();

            if (enable)
            {
                Camera cam = Camera.GetCameraInstance();
                Vector3 position = cam.GetVector3Position();
                Vector3 dir = cam.GetVector3LookAt() - position;
                dir.Normalize();

                Scene.SceneManager.SceneObject pl = sceneManager.GetObject("Hrac");

                if (pl != null)
                {
                    player = (Objects.Player)pl.generalObject;

                    if ((Scene.SceneManager.DetailLevel)Properties.Settings.Default.DetailLevel == Scene.SceneManager.DetailLevel.Low)
                    {
                        player.SetModel(null);
                    }
                }
                else
                {
                    sceneManager.AddObject("Hrac", player, (Effect)null);
                }

                Vector3 playerPosition = position;
                playerPosition.Y -= HeightOfEye.Y;
                playerPosition -= HeightOfEye.Z * dir;
                player.SetPosition(playerPosition);

                SceneManager.SceneObject pochoden = sceneManager.GetObject("Hrac_Pochoden");

                if (pochoden != null)
                {
                    player.EquipItem(pochoden.generalObject);
                }

            }
            else
            {
                if (player != null)
                {
                    player.SetMatrixWorld(originalPlayerWorld);
                    sceneManager.DeleteObject("Hrac");
                }
            }
        }

        /// <summary>
        /// Provede svazani kamery s terenem, po kterem je mozno se pohybovat
        /// </summary>
        /// <param name="terain"></param>
        public static void SetAttachedTerain(WiccanRede.AI.IWalkable terain)
        {
            CameraDriver.terain = terain;
        }

        /// <summary>
        /// Vrati objekt terenu, po kterem se hrac pohybuje
        /// </summary>
        /// <returns>Vrati objekt terenu, po kterem se hrac pohybuje</returns>
        public static AI.IWalkable GetAttachedTerain()
        {
            return CameraDriver.terain;
        }

        /// <summary>
        /// Povoli volny pohyb bez aplikovani fyziky
        /// </summary>
        public static void EnableFreeLook()
        {
            CameraDriver.freeLook = true;

            Speed = 0.8f;

            SetShowPlayer(!CameraDriver.freeLook);
        }

        /// <summary>
        /// Zrusi volny pohyb a zacne aplikovat fyzikalni pravidla
        /// </summary>
        public static void DisableFreeLook()
        {
            if (terain != null)
            {
                CameraDriver.freeLook = false;
                Speed = 0.2f;
            }
            else
                CameraDriver.freeLook = true;

            SetShowPlayer(!CameraDriver.freeLook);
        }

        /// <summary>
        /// Nastavi citlivost mysi
        /// </summary>
        /// <param name="sensitivity">Citlivost</param>
        public static void SetSensitivity(float sensitivity)
        {
            CameraDriver.Sensitivity = sensitivity;
        }

        /// <summary>
        /// Vrati citlivost mysi
        /// </summary>
        /// <returns>Vrati citlivost mysi</returns>
        public static float GetSensitivity()
        {
            return Sensitivity;
        }

        /// <summary>
        /// Funkce nastavi aktualni cas
        /// </summary>
        /// <param name="time">Novy cas</param>
        public static void SetTime(int time)
        {
            CameraDriver.time = time;
        }

        /// <summary>
        /// Funkce vrati naposled nastaveny cas
        /// </summary>
        /// <returns>Funkce vrati naposled nastaveny cas</returns>
        public static int GetTime()
        {
            return time;
        }

        /// <summary>
        /// Funkce nastavi rychlost hrace
        /// </summary>
        /// <param name="speed">Rychlost hrace</param>
        public static void SetSpeed(float speed)
        {
            CameraDriver.Speed = speed;
        }

        /// <summary>
        /// Funkce vrati aktualni rychlost hrace
        /// </summary>
        /// <returns>Funkce vrati aktualni rychlost hrace</returns>
        public static float GetSpeed()
        {
            return Speed;
        }

        /// <summary>
        /// Nastavi invertovani mysi
        /// </summary>
        /// <param name="inverted">Pokud je true, tak je mys invertovana, jinak je normalni</param>
        public static void SetInvertedMouse(bool inverted)
        {
            CameraDriver.invertedmouse = inverted;
        }

        /// <summary>
        /// Vrati, zda je mys invertovana
        /// </summary>
        /// <returns>Pokud je vraceno true, je mys invertovana</returns>
        public static bool GetInvertedMouse()
        {
            return CameraDriver.invertedmouse;
        }

        /// <summary>
        /// Presune hrace na startovani pozici (0,0,0)
        /// </summary>
        public static void MoveToStartPosition()
        {
            Vector3 pos = new Vector3(-3000, 0, -3000);
            pos += HeightOfEye;

            Camera cam = Camera.GetCameraInstance();

            Vector3 dir = cam.GetVector3LookAt() - cam.GetVector3Position();
            dir.Y = 0;

            RepairPositionInNonFreeLook(pos, ref pos);

            //if (freeLook)
            //  pos += new Vector3(0, 1800, 0);

            cam.SetCameraPositionVector(pos);
            cam.SetCameraLookAtVector(pos + dir);
        }

        /// <summary>
        /// Pohne s hracem dopredu o jeden krok
        /// </summary>
        public static void MoveForward()
        {
            MoveByAngle(0f);
        }

        /// <summary>
        /// Pohne s hracem dozadu o jeden krok
        /// </summary>
        public static void MoveBackward()
        {
            MoveByAngle((float)Math.PI);

        }

        /// <summary>
        /// Pohne s hracem o jeden krok ve smeru zadaneho uhlu
        /// </summary>
        /// <param name="angle">Uhel v radianech urcujici smer pohybu</param>
        public static void MoveByAngle(float angle)
        {
            Camera cam = Camera.GetCameraInstance();

            Vector3 lookat = cam.GetVector3LookAt();
            Vector3 position = cam.GetVector3Position();

            Matrix rotation = Matrix.RotationY(angle);
            Vector3 dir = lookat - position;

            if (freeLook)
            {
                dir.Normalize();
                dir.TransformCoordinate(rotation);
                dir.Normalize();

                if (Math.Abs(angle - Math.PI) < 0.5)
                    dir.Y = -dir.Y;
                else if (Math.Abs(angle - Math.PI / 2f) < 0.5 || Math.Abs(angle - Math.PI / 2f + Math.PI) < 0.5)
                    dir.Y = 0;

                Vector3 newposition = position + (Speed * time) * dir;
                Vector3 newlookAt = lookat + (Speed * time) * dir;

                cam.SetCameraLookAtVector(newlookAt);
                cam.SetCameraPositionVector(newposition);
                return;
            }


            dir.Y = 0;
            dir.Normalize();

            dir.TransformCoordinate(rotation);
            dir.Normalize();

            Vector3 newPosition = position + (Speed * time) * dir;

            RepairPositionInNonFreeLook(position, ref newPosition);

            if (position == newPosition)
                return;

            Vector3 newLookAt = lookat + newPosition - position;

            cam.SetCameraLookAtVector(newLookAt);
            cam.SetCameraPositionVector(newPosition);

            if (player == null)
                return;

            dir = newLookAt - newPosition;
            dir.Y = 0;
            dir.Normalize();

            Vector3 playerPosition = newPosition;
            playerPosition.Y -= HeightOfEye.Y;
            playerPosition -= HeightOfEye.Z * dir;

            player.SetPosition(playerPosition);
        }


        /// <summary>
        /// Pohne s hracem ve smeru leveho ukroku
        /// </summary>
        public static void StrafeLeft()
        {
            MoveByAngle(-(float)Math.PI / 2f);
        }

        /// <summary>
        /// Pohne s hracem ve smeru praveho ukroku
        /// </summary>
        public static void StrafeRight()
        {
            MoveByAngle((float)Math.PI / 2f);
        }

        /// <summary>
        /// Necha hrace rozhlizet se
        /// </summary>
        /// <param name="x">Vychyleni v horizontalnim smeru</param>
        /// <param name="y">Vychyleni ve vertikalnim smeru</param>
        public static void LookAround(float x, float y)
        {
            Camera cam = Camera.GetCameraInstance();

            Vector3 lookat = cam.GetVector3LookAt();
            Vector3 position = cam.GetVector3Position();


            Vector3 dir = lookat - position;

            dir.Normalize();

            Vector3 ndir = Vector3.Cross(dir, cam.GetVector3Up());
            ndir.Normalize();

            Vector3 undir = Vector3.Cross(dir, ndir);
            undir.Normalize();

            float invert = 1;

            if (invertedmouse)
                invert *= -1;

            Vector3 dx = -(Sensitivity / 2000f) * x * ndir;
            Vector3 dy = invert * (Sensitivity / 2000f) * y * undir;

            Vector3 newLookAt = lookat;

            newLookAt = newLookAt + dx;
            newLookAt = newLookAt + dy;

            Vector3 newdir = newLookAt - position;
            newdir.Normalize();

            lookat = position + newdir;

            if (newdir.Y > -0.9999f && newdir.Y < 0.9999f)
                cam.SetCameraLookAtVector(lookat);

            if (player == null)
                return;

            newdir = cam.GetVector3LookAt() - cam.GetVector3Position();
            newdir.Y = 0;
            newdir.Normalize();

            Vector3 positionPlayer = cam.GetVector3Position();
            positionPlayer.Y -= HeightOfEye.Y;
            positionPlayer -= HeightOfEye.Z * newdir;

            player.SetPosition(positionPlayer);
            player.SetDirection(newdir);
        }

        /// <summary>
        /// Pokud neni volny pohyb, tak opravi vyskovou pozici hrace tak, aby hrac stal na terenu
        /// </summary>
        /// <param name="position">Puvodni pozice hrace</param>
        /// <param name="newPosition">Pozice hrace, na kterou chce hrac jit, tato promena bude prepsana korektni hodnotou</param>
        private static void RepairPositionInNonFreeLook(Vector3 position, ref Vector3 newPosition)
        {
            if (!freeLook)
            {
                Scene.SceneManager sm = GraphicCore.GetCurrentSceneManager();
                List<SceneManager.SceneObject> sos = sm.GetAllObjects();

                Vector3 dir = Vector3.Normalize(newPosition - position) * Speed * time;

                bool blocked = false;
                foreach (SceneManager.SceneObject so in sos)
                {
                    if (so.name.StartsWith("Hrac"))
                        continue;

                    if (so.generalObject.ComputeBoxCollission(newPosition) ||
                        so.generalObject.ComputeBoxCollission(newPosition + dir * 0.02f) ||
                        so.generalObject.ComputeBoxCollission(newPosition + dir * 0.01f) ||
                        so.generalObject.ComputeBoxCollission(newPosition + dir * 0.005f) ||
                        so.generalObject.ComputeBoxCollission(newPosition + dir * 0.0025f) ||
                        so.generalObject.ComputeBoxCollission(newPosition + dir * 0.00125f))
                    {
                        blocked = true;
                    }
                }

                if (terain.IsPositionOnTereain(newPosition) && !blocked /*!terain.IsPositionOnTerainBlocked(newPosition)*/)
                {
                    newPosition = terain.GetPositionOnTerain(newPosition);
                }
                else
                    newPosition = terain.GetPositionOnTerain(position);

                newPosition.Y += HeightOfEye.Y;
            }
        }

        /// <summary>
        /// Metoda vrati matici s orthogonalni projekci terenu
        /// </summary>
        /// <returns>Metoda vrati matici s orthogonalni projekci terenu</returns>
        public static Matrix GetTerainOrthoProjectionMatrix()
        {
            if (terain != null)
            {
                Vector2 vs = terain.GetTerrainSize();
                return Matrix.OrthoLH(vs.Y, vs.X, 0, vs.Y);
            }

            return Matrix.Identity;
        }

        /// <summary>
        /// Metoda vrati view matici ze zadaneho mista pohledu do stredu terenu
        /// </summary>
        /// <param name="position">Misto pozorovatele</param>
        /// <returns>Vrati view matici</returns>
        public static Matrix GetTerainViewMatrix(Vector3 position)
        {
            if (terain != null)
            {
                Vector2 ts = terain.GetTerrainSize();
                return Matrix.LookAtLH(0.5f * ts.Y * Vector3.Normalize(position), terain.GetPositionOnTerain(new Vector3(0, 0, 0)), new Vector3(0, 1, 0));
            }

            return Matrix.Identity;
        }

        /// <summary>
        /// Metoda vrati view matici ze zadaneho mista pohledu do stredu terenu
        /// </summary>
        /// <param name="position">Misto pozorovatele</param>
        /// <param name="upVector">Znaci, kde je pro kameru "nahoru"</param>
        /// <returns>Vrati view matici</returns>
        public static Matrix GetTerainViewMatrix(Vector3 position, Vector3 upVector)
        {
            if (terain != null)
            {
                Vector2 ts = terain.GetTerrainSize();
                return Matrix.LookAtLH(0.5f * ts.Y * Vector3.Normalize(position), terain.GetPositionOnTerain(new Vector3(0, 0, 0)), upVector);
            }

            return Matrix.Identity;
        }

        /// <summary>
        /// Vrati view * ortho projection matici na terenu
        /// </summary>
        /// <param name="position">Pozice pozorovatele</param>
        /// <returns>Vrati view * ortho projection matici na terenu</returns>
        public static Matrix GetTerainViewOrthoProjectionMatrix(Vector3 position)
        {
            return GetTerainViewMatrix(position) * GetTerainOrthoProjectionMatrix();
        }
    }

}
