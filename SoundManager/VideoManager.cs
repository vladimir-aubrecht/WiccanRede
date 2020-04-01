using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX.AudioVideoPlayback;
using System.Windows.Forms;
using System.Drawing;

namespace WiccanRede.Multimedia
{
    public class VideoManager : IDisposable
    {
        private Form form;
        private Size size;
        private bool fullscreen = false;

        Video video;

        public void Dispose()
        {
            if (video != null && !video.Disposed)
            video.Dispose();
        }

        public VideoManager(Form form, bool fullscreen)
        {
            this.form = form;
            this.size = form.Size;
            this.fullscreen = fullscreen;
        }

        public void LoadVideo(String path, bool loop)
        {
            video = new Video(path, false);
            video.Fullscreen = this.fullscreen;
            video.Owner = form;
            video.Size = this.size;

            if (loop)
                video.Ending += new EventHandler(video_Ending);
        }

        void video_Ending(object sender, EventArgs e)
        {
            video.Stop();
            video.Play();
        }

        public void PlayVideo()
        {
            video.Play();
        }

        public void StopVideo()
        {
            video.Stop();
        }


    }
}
