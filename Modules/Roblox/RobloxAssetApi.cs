using System.IO.Compression;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Commands;

namespace Modules.Roblox
{
    public class RobloxAssetApi : RobloxMainApi
    {
        // prop
        public RobloxAssetApi(CommandBase.IKey Key) : base(Key) {}

        // types
        public record TexStruct {
            public string[] Hash {get; set;} = null!;
            public string[] Url {get; set;} = null!;
        }
        public record MeshData
        {
            public string Obj {get; set;} = null!;
            public string Mtl {get; set;} = null!;
            public TexStruct Tex {get; set;} = null!;
        }

        // user
        public async Task<int> GetUIDbyUsername(string Username)
        {
            var Api = "https://users.roblox.com/v1/usernames/users";
            var Payload = new
            {
                Usernames = new[]
                {
                    Username
                },
                ExcludeBannedUsers = false
            };

            // act
            var Resp = await Http.PostAsJsonAsync(Api, Payload);
            var JsonString = await Resp.Content.ReadAsStringAsync();
            var JsonData = JsonSerializer.Deserialize<JsonObject>(JsonString);

            if(JsonData!["data"]!.AsArray().Count < 1)
            {
                throw new Exception("User not found!");
            }

            return JsonData["data"]![0]!["id"]!.GetValue<int>();
        }
        public async Task<JsonNode> UidDetail(int UserId)
        {
            var Api = $"https://users.roblox.com/v1/users/{UserId}";

            var Resp = await Http.GetAsync(Api);
            var JsonString = await Resp.Content.ReadAsStringAsync();
            var JsonData = JsonSerializer.Deserialize<JsonObject>(JsonString);

            var UserShot = await GetUserShot(UserId);
            JsonData!["imageUrl"] = UserShot;

            return JsonData!;
        }
        public async Task<string> GetUserShot(int UserId)
        {
            var Api = "https://thumbnails.roblox.com/v1/batch";
            var BodyBatch = new[]
            {
                new
                {
                    RequestId = $"{UserId}::Avatar:352x352:webp:regular",
                    Type = "Avatar",
                    TargetId = UserId,
                    Token = "",
                    Format = "webp",
                    Size = "352x352"
                }
            };

            var Resp = await Http.PostAsJsonAsync(Api, BodyBatch);
            var JsonString = await Resp.Content.ReadAsStringAsync();
            var JsonData = JsonSerializer.Deserialize<JsonObject>(JsonString);

            return JsonData!["data"]![0]!["imageUrl"]!.GetValue<string>();
        }
        public async Task<MeshData> GetUserObj(int UserId)
        {
            var Api = $"https://thumbnails.roblox.com/v1/users/avatar-3d?userId={UserId}";

            // BatchRes
            var BatchResp = await Http.GetAsync(Api);
            var BatchJsonString = await BatchResp.Content.ReadAsStringAsync();
            var BatchJsonData = JsonSerializer.Deserialize<JsonObject>(BatchJsonString);

            // ObjData 
            var ObjResp = await Http.GetAsync(BatchJsonData!["imageUrl"]!.GetValue<string>());
            var ObjJsonString = await ObjResp.Content.ReadAsStringAsync();
            var ObjJsonData = JsonSerializer.Deserialize<JsonObject>(ObjJsonString);

            var TexUrls = ObjJsonData!["textures"]!.AsArray().ToList().Select(sel => GetHashUrl(sel!.GetValue<string>()));
            var TexArr = new TexStruct
            {
                Hash = ObjJsonData["textures"]!.AsArray().Select(sel => sel!.GetValue<string>()).ToArray(),
                Url = TexUrls.ToArray()
            };
            
            var ModelResp = new MeshData
            {
                Obj = GetHashUrl(ObjJsonData["obj"]!.GetValue<string>()),
                Mtl = GetHashUrl(ObjJsonData["mtl"]!.GetValue<string>()),
                Tex = TexArr
            };

            return ModelResp;
        }
        public async Task<string> GetUserIdAvatarType(int UserId)
        {
            var Api = $"https://avatar.roblox.com/v1/users/{UserId}/avatar";

            var Resp = await Http.GetAsync(Api);
            var JsonString = await Resp.Content.ReadAsStringAsync();
            var JsonData = JsonSerializer.Deserialize<JsonObject>(JsonString);

            return JsonData!["playerAvatarType"]!.GetValue<string>();
        }
        public async Task<string> ZipUserObjToBuffer(string Username)
        {
            var ZipMemory = new MemoryStream();
            using (var Zip = new ZipArchive(ZipMemory, ZipArchiveMode.Create))
            {
                // get uid
                var UserId = await GetUIDbyUsername(Username);
                var UserDetail = await UidDetail(UserId);

                // get user obj
                var UserObj = await GetUserObj(UserId);

                // Mesh Data
                var ObjResp = await Http.GetAsync(UserObj.Obj);
                var ObjData = await ObjResp.Content.ReadAsStringAsync();

                var MtlResp = await Http.GetAsync(UserObj.Mtl);
                var MtlDataRaw = await MtlResp.Content.ReadAsStringAsync();

                var UrlsTex = UserObj.Tex.Url;
                var HashedTex = UserObj.Tex.Hash;
                var DeHashTex = HashedTex.Select((value, index) =>
                {
                    index++;
                    return $"{Username}_Tex{index}.png";
                });
                var MtlData = StrReplace(HashedTex.ToArray(), DeHashTex.ToArray(), MtlDataRaw);

                // TexFile to Zip
                foreach (var (data, index) in UrlsTex.Select((sel, index) => (sel, index)))
                {
                    var TexResp = await Http.GetAsync(data);
                    var TexData = await TexResp.Content.ReadAsByteArrayAsync();

                    // create zip entry
                    var ZipEntryTex = Zip.CreateEntry(DeHashTex.ElementAt(index));

                    // write to stream writer of zip entry
                    using (var ZipTexStream = ZipEntryTex.Open()) {
                        await ZipTexStream.WriteAsync(TexData, 0, TexData.Length);
                    }
                }

                // Obj And Mtl Write to Zip
                var ZipEntryObj = Zip.CreateEntry($"{Username}.obj");
                using (var ZipObjStream = new StreamWriter(ZipEntryObj.Open()))
                {
                    ZipObjStream.Write(ObjData);
                }
                
                var ZipEntryMtl = Zip.CreateEntry($"{Username}.mtl");
                using (var ZipMtlStream = new StreamWriter(ZipEntryMtl.Open()))
                {
                    ZipMtlStream.Write(MtlData);
                }
            }

            var ZipBase64 = Convert.ToBase64String(ZipMemory.ToArray());
            return $"data:application/zip;base64,{ZipBase64}";
        }
        public async Task<string> ZipUserIdObjToBuffer(int UserId)
        {
            var ZipMemory = new MemoryStream();
            using (var Zip = new ZipArchive(ZipMemory, ZipArchiveMode.Create))
            {
                // get uid
                var UserDetail = await UidDetail(UserId);

                // get user obj
                var UserObj = await GetUserObj(UserId);

                // Mesh Data
                var ObjResp = await Http.GetAsync(UserObj.Obj);
                var ObjData = await ObjResp.Content.ReadAsStringAsync();

                var MtlResp = await Http.GetAsync(UserObj.Mtl);
                var MtlDataRaw = await MtlResp.Content.ReadAsStringAsync();

                var UrlsTex = UserObj.Tex.Url;
                var HashedTex = UserObj.Tex.Hash;
                var DeHashTex = HashedTex.Select((value, index) =>
                {
                    index++;
                    return $"{UserId}_Tex{index}.png";
                });
                var MtlData = StrReplace(HashedTex.ToArray(), DeHashTex.ToArray(), MtlDataRaw);

                // TexFile to Zip
                foreach (var (data, index) in UrlsTex.Select((sel, index) => (sel, index)))
                {
                    var TexResp = await Http.GetAsync(data);
                    var TexData = await TexResp.Content.ReadAsByteArrayAsync();

                    // create zip entry
                    var ZipEntryTex = Zip.CreateEntry(DeHashTex.ElementAt(index));

                    // write to stream writer of zip entry
                    using (var ZipTexStream = ZipEntryTex.Open()) {
                        await ZipTexStream.WriteAsync(TexData, 0, TexData.Length);
                    }
                }

                // Obj And Mtl Write to Zip
                var ZipEntryObj = Zip.CreateEntry($"{UserId}.obj");
                using (var ZipObjStream = new StreamWriter(ZipEntryObj.Open()))
                {
                    ZipObjStream.Write(ObjData);
                }
                
                var ZipEntryMtl = Zip.CreateEntry($"{UserId}.mtl");
                using (var ZipMtlStream = new StreamWriter(ZipEntryMtl.Open()))
                {
                    ZipMtlStream.Write(MtlData);
                }
            }

            var ZipBase64 = Convert.ToBase64String(ZipMemory.ToArray());
            return $"data:application/zip;base64,{ZipBase64}";
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