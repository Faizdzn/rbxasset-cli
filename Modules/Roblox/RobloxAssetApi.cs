using Commands;

namespace Modules.Roblox
{
    public class RobloxAssetApi : RobloxMainApi
    {
        public RobloxAssetApi(CommandBase.IKey Key) : base(Key) {}
        
        // group
        public async Task getGroupDetail(int GroupId)
        {
            
        }
        public async Task getGroupGames(int GroupId)
        {
            
        }
        public async Task getGroupIcon(int GroupId)
        {
            
        }
        
        // game
        public async Task getGameDetail(int UniverseId)
        {
            
        }
        public async Task getGameIcon (int UniverseId)
        {
            
        }

        // user
        public async Task getUIDbyUsername(string Username)
        {
            
        }
        public async Task UidDetail(int UserId)
        {
            
        }
        public async Task GetUserShot(int UserId)
        {
            
        }
        public async Task GetUserObj(int UserId)
        {
            
        }
        public async Task GetUserIdAvatarType(int UserId)
        {
            
        }
        public async Task ZipUserObjToBuffer(string Username)
        {
            
        }
        public async Task ZipUserIdObjToBuffer(int UserId)
        {
            
        }

        // item
        public async Task ItemDetail(int ItemId)
        {
            
        }
        public async Task GetItemObj(int ItemId)
        {
            
        }
        public async Task ZipItemObjToBuffer(int ItemId)
        {
            
        }

        // bundle
        public async Task BundleDetail(int BundleId)
        {
            
        }
        public async Task GetBundleIdObj(int OutfitId)
        {
            
        }
        public async Task ZipBundleObjToBuffer(int BundleId)
        {
            
        }
    }
}