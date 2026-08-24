namespace Actions
{
    public static class ImageAction
    {
        public static async Task Run(string ApiKey, int ImageId)
        {
            Console.WriteLine($"{ApiKey} {ImageId}");
        }
    }
}