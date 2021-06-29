using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup
{
    public class MidiFileMetadata
    {
        public string FileName { get; private set; }
        public DateTime RecordDate { get; private set; }
        public double Duration { get; private set; }

        public DateTime LastUpdated { get; private set; }

        public bool FileExists
            => File.Exists($"{MidiFileManager.MidiFileDirectory}/{FileName}");

        public MidiFileMetadata() { }

        public MidiFileMetadata(string filename, double duration)
        {
            this.RecordDate = DateTime.UtcNow;
            this.FileName = filename;
            this.Duration = duration;
            this.LastUpdated = DateTime.UtcNow;
        }

        public MidiFileMetadata Update(string name = null, double duration = 0)
        {
            this.FileName = name ?? this.FileName;
            this.Duration = duration == 0 ? this.Duration : duration;
            this.LastUpdated = DateTime.UtcNow;

            return this;
        }

        public MidiFileMetadata Clone()
            => this.MemberwiseClone() as MidiFileMetadata;
    }

    public class MidiFileManager
    {
        public static string MidiFileDirectory { get; } = $"{Environment.CurrentDirectory}/MidiFiles";
        public static string MidiMetaPath { get; } = $"{Environment.CurrentDirectory}/midi.meta";


        public IReadOnlyCollection<MidiFileMetadata> Files
            => _files;

        private List<MidiFileMetadata> _files { get; set; } = new();
        private MidiDriver Driver { get; }
        private FileSystemWatcher Watcher { get; }

        public MidiFileManager(MidiDriver driver)
        {
            this.Driver = driver;

            LoadMetadata();

            Watcher = new(MidiFileDirectory);

            Watcher.Filter = "*.midi";

            Watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime;

            Watcher.EnableRaisingEvents = true;

            Watcher.Created += Watcher_Created;
            Watcher.Deleted += Watcher_Deleted;
            Watcher.Changed += Watcher_Changed;
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            var meta = _files.FirstOrDefault(x => x.FileName == e.Name);

            if (meta == null)
                return;

            MidiFile file = null;

            try
            {
                file = MidiFile.Read(e.FullPath);
            }
            catch(Exception x)
            {
                Logger.Debug($"File {e.Name} was invalid for reading: {x}", Severity.FileManager);
                _files.Remove(meta);
                SaveMetadata();
                return;
            }

            var duration = file.GetFileDuration();

            if (_files.Replace(meta, meta.Clone().Update(e.Name, duration.TotalSeconds)) == 0)
                Logger.Debug($"Update returned 0 for {e.Name}", Severity.FileManager, Severity.Warning);
            else SaveMetadata();
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            var meta = _files.FirstOrDefault(x => x.FileName == e.Name);

            if (meta == null)
                return;

            Logger.Write($"File {meta.FileName} was deleted", Severity.FileManager, Severity.Log);

            _files.Remove(meta);
            SaveMetadata();
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            if (_files.Any(x => x.FileName == e.Name))
            {
                Logger.Write($"File already exists in meta: {e.Name}, Ignoring", Severity.FileManager, Severity.Warning);
                return;
            }

            MidiFile midi = null;

            try
            {
                midi = MidiFile.Read(e.FullPath);
            }
            catch(Exception x)
            {
                Logger.Write($"File {e.Name} was invalid for reading: {x}", Severity.FileManager, Severity.Warning);
                return;
            }

            var duration = midi.GetFileDuration();

            var meta = new MidiFileMetadata(e.Name, duration.TotalSeconds);
            _files.Add(meta);
            SaveMetadata();
        }

        public void LoadMetadata()
        {
            if (!File.Exists(MidiMetaPath))
            {
                File.Create(MidiMetaPath).Close();

                var files = Directory.GetFiles(MidiFileDirectory);

                foreach (var file in files)
                {
                    Watcher_Created(null, new FileSystemEventArgs(WatcherChangeTypes.Created, MidiFileDirectory, file.Split("/").Last()));
                }
            }

            this._files = JsonConvert.DeserializeObject<List<MidiFileMetadata>>(File.ReadAllText(MidiMetaPath));
        }

        public void SaveMetadata()
        {
            var json = JsonConvert.SerializeObject(Files);

            File.WriteAllText(MidiMetaPath, json);
        }
    }

    public static class Extensions
    {
        public static TimeSpan GetFileDuration(this MidiFile file)
        {
            return file.GetTimedEvents()
                    .LastOrDefault(e => e.Event is NoteOffEvent)
                    ?.TimeAs<MetricTimeSpan>(file.GetTempoMap() ?? default) ?? new MetricTimeSpan();
        }

        public static int Replace<T>(this IList<T> source, T oldValue, T newValue)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var index = source.IndexOf(oldValue);
            if (index != -1)
                source[index] = newValue;
            return index;
        }

        public static void ReplaceAll<T>(this IList<T> source, T oldValue, T newValue)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            int index = -1;
            do
            {
                index = source.IndexOf(oldValue);
                if (index != -1)
                    source[index] = newValue;
            } while (index != -1);
        }


        public static IEnumerable<T> Replace<T>(this IEnumerable<T> source, T oldValue, T newValue)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.Select(x => EqualityComparer<T>.Default.Equals(x, oldValue) ? newValue : x);
        }

        public static int Replace<T>(this IList<T> source, Func<T, bool> filter, T value) where T : class
        {
            var old = source.FirstOrDefault(x => filter(x));

            if (old == default)
                return 0;

            return Replace<T>(source, old, value);
        }
    }
}
