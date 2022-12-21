namespace TinderButForBarteringBackend
{
    class Product
    {
        public int Id { get; set; }
        //public int OwnerId { get; set; }
        //public string Category { get; set; }
        //public picture PrimaryPicture { get; set; }
        //public string Description { get; set; }
        //public picture[] AdditionalPictures { get; set; }
        //public bool IsSold { get; set; }


        public string Title { get; set; }
        public string Description { get; set; }
        public bool RequiresSomethingInReturn { get; set; }
        public byte[] PrimaryPictureData { get; set; }
    }
}
