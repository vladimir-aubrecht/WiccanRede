using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX;
using Microsoft.DirectX.DirectSound;
using Microsoft.DirectX.AudioVideoPlayback;
using System.Windows.Forms;

namespace WiccanRede.Multimedia
{
    /// <summary>
    /// Trida obsluhuje Audio/Video vystup
    /// </summary>
    public class SoundManager : IDisposable
    {
        Device soundDevice;

        int currentSong = 0;
        List<Audio> playlist = new List<Audio>();

        /// <summary>
        /// Metoda se postara o uklizeni pameti
        /// </summary>
        public void Dispose()
        {
            if (soundDevice != null && !soundDevice.Disposed)
                soundDevice.Dispose();

            foreach (Audio audio in playlist)
            {
                if (audio != null && !audio.Disposed)
                    audio.Dispose();
            }

        }

        /// <summary>
        /// Vytvori novou instanci tridu MultimediaManager
        /// </summary>
        /// <param name="form">Okno, ke kteremu jsou audio/video sluzby navazany</param>
        public SoundManager(Form form)
        {
            soundDevice = new Device();
            soundDevice.SetCooperativeLevel(form, CooperativeLevel.Normal);
            
        }

        /// <summary>
        /// Metoda prida do playlistu hudbu ze zadaneho souboru nebo slozky
        /// </summary>
        /// <param name="file">Cesta k souboru nebo slozce s hudbou</param>
        public void AddToPlayList(String file)
        {
            String[] files = null;
            if (System.IO.File.GetAttributes(file) == System.IO.FileAttributes.Directory)
            {
                files = System.IO.Directory.GetFiles(file, "*.mp3");
            }

            if (files != null)
            {
                foreach (String s in files)
                {
                    Audio audio = new Audio(s, false);
                    audio.Ending += new EventHandler(audio_Ending);
                    playlist.Add(audio);
                }
            }
            else
            {
                //Audio audio = new Audio(file, false);
                //audio.Ending += new EventHandler(audio_Ending);
                //playlist.Add(audio);
            }
        }

        /// <summary>
        /// Zastavi prehravani hudby
        /// </summary>
        public void StopMusic()
        {
            if (playlist.Count > currentSong) 
                playlist[currentSong].Stop();
        }

        /// <summary>
        /// Spusti prehravani hudby od naposled prehrane skladby
        /// </summary>
        public void PlayMusic()
        {
            if (playlist.Count > currentSong) 
                playlist[currentSong].Play();
        }

        private void audio_Ending(object sender, EventArgs e)
        {
            StopMusic();

            currentSong++;

            if (currentSong >= playlist.Count)
                currentSong = 0;

            PlayMusic();
        }

    }

}
