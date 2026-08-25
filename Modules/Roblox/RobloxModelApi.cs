using Commands;

namespace Modules.Roblox {
    public class RobloxModelApi : RobloxMainApi
    {
        public RobloxModelApi(CommandBase.IKey Key) : base(Key) {}
        
        // api section
        public async Task RequestAssetApi(long AssetId)
        {
            
        }

        // buffers
        public async Task LoadFtsDirect(string FtsUrl)
        {
            
        }
        // api section
        public async Task MeshParser(byte[] buffer)
        {
            
        }

        // rbxm
        public async Task GetRbxmFile(int ModelId)
        {
            
        }
        public async Task GetMeshFile(long MeshId)
        {
            
        }
        public async Task GetImageFile(long ImageId)
        {
            
        }

        // parse rbxassetid://
        public async Task ParseRbxAssetId(string Url)
        {
            
        }
    }
}