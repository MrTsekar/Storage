namespace Storage.Models
{
    public class FileLocation
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public string FilePath { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
