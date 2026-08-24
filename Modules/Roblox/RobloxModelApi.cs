using Commands;

namespace Modules.Roblox {
    public class RobloxModelApi : RobloxMainApi
    {
        public RobloxModelApi(CommandBase.IKey Key) : base(Key) {}
        
        // api section
        public async Task RequestAssetApi(int AssetId)
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
        public async Task GetMeshFile(int MeshId)
        {
            
        }
        public async Task GetImageFile(int ImageId)
        {
            
        }

        // parse rbxassetid://
        public async Task ParseRbxAssetId(string Url)
        {
            
        }
    }
}