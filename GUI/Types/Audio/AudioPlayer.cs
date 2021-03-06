using System;
using System.Drawing;
using System.Windows.Forms;
using NAudio.Wave;
using NLayer.NAudioSupport;
using ValveResourceFormat;
using ValveResourceFormat.ResourceTypes;

namespace GUI.Types.Audio
{
    internal class AudioPlayer
    {
        private readonly Button playButton;
        private WaveOutEvent waveOut;

        public AudioPlayer(Resource resource, TabPage tab)
        {
            var soundData = (Sound)resource.Blocks[BlockType.DATA];

            var stream = soundData.GetSoundStream();
            waveOut = new WaveOutEvent();

            try
            {
                if (soundData.Type == Sound.AudioFileType.WAV)
                {
                    var rawSource = new WaveFileReader(stream);
                    waveOut.Init(rawSource);
                }
                else if (soundData.Type == Sound.AudioFileType.MP3)
                {
                    var builder = new Mp3FileReader.FrameDecompressorBuilder(wf => new Mp3FrameDecompressor(wf));
                    var rawSource = new Mp3FileReader(stream, builder);
                    waveOut.Init(rawSource);
                }
                else if (soundData.Type == Sound.AudioFileType.AAC)
                {
                    var rawSource = new StreamMediaFoundationReader(stream);
                    waveOut.Init(rawSource);
                }

                playButton = new Button();
                playButton.Text = "Play";
                playButton.TabIndex = 1;
                playButton.Size = new Size(100, 25);
                playButton.Click += PlayButton_Click;
                playButton.Disposed += PlayButton_Disposed;

                tab.Controls.Add(playButton);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);

                var msg = new Label
                {
                    Text = $"NAudio Exception: {e.Message}",
                    Dock = DockStyle.Fill,
                };

                tab.Controls.Add(msg);
            }
        }

        private void PlayButton_Disposed(object sender, EventArgs e)
        {
            if (waveOut != null)
            {
                Console.WriteLine("Disposed sound");
                waveOut.Dispose();
                waveOut = null;
            }
        }

        private void PlayButton_Click(object sender, EventArgs e)
        {
            if (waveOut.PlaybackState == PlaybackState.Playing)
            {
                waveOut.Pause();
                playButton.Text = "Play";
            }
            else
            {
                waveOut.Play();
                playButton.Text = "Pause";
            }
        }
    }
}
