namespace Actions
{
    public static class MeshAction
    {
        public static async Task Run(string ApiKey, int MeshId)
        {
            Console.WriteLine($"{ApiKey} {MeshId}");
        }
    }
}