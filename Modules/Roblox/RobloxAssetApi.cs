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
        public record PosStruct
        {
            public float X {get; set;}
            public float Y {get; set;}
            public float Z {get; set;}
        }
        public record MeshData
        {
            public string Obj {get; set;} = null!;
            public string Mtl {get; set;} = null!;
            public TexStruct Tex {get; set;} = null!;
            public PosStruct? Pos {get; set;}
        }

        // user
        public async Task<long> GetUIDbyUsername(string Username)
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
            var JsonData = await Resp.Content.ReadFromJsonAsync<JsonObject>();

            if(JsonData!["data"]!.AsArray().Count < 1)
            {
                throw new Exception("User not found!");
            }

            return JsonData["data"]![0]!["id"]!.GetValue<long>();
        }
        public async Task<JsonObject> UidDetail(long UserId)
        {
            var Api = $"https://users.roblox.com/v1/users/{UserId}";

            var Resp = await Http.GetAsync(Api);
            var JsonData = await Resp.Content.ReadFromJsonAsync<JsonObject>();

            var UserShot = await GetUserShot(UserId);
            JsonData!["imageUrl"] = UserShot;

            return JsonData!;
        }
        public async Task<string> GetUserShot(long UserId)
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
            var JsonData = await Resp.Content.ReadFromJsonAsync<JsonObject>();

            return JsonData!["data"]![0]!["imageUrl"]!.GetValue<string>();
        }
        public async Task<MeshData> GetUserObj(long UserId)
        {
            var Api = $"https://thumbnails.roblox.com/v1/users/avatar-3d?userId={UserId}";

            // BatchRes
            var BatchResp = await Http.GetAsync(Api);
            var BatchJsonData = await BatchResp.Content.ReadFromJsonAsync<JsonObject>();

            // MeshData 
            var MeshResp = await Http.GetAsync(BatchJsonData!["imageUrl"]!.GetValue<string>());
            var MeshJsonData = await MeshResp.Content.ReadFromJsonAsync<JsonObject>();

            var TexUrls = MeshJsonData!["textures"]!.AsArray().Select(sel => GetHashUrl(sel!.GetValue<string>()));
            var TexArr = new TexStruct
            {
                Hash = MeshJsonData["textures"]!.AsArray().Select(sel => sel!.GetValue<string>()).ToArray(),
                Url = TexUrls.ToArray()
            };
            
            var ModelResp = new MeshData
            {
                Obj = GetHashUrl(MeshJsonData["obj"]!.GetValue<string>()),
                Mtl = GetHashUrl(MeshJsonData["mtl"]!.GetValue<string>()),
                Tex = TexArr
            };

            return ModelResp;
        }
        public async Task<string> GetUserIdAvatarType(long UserId)
        {
            var Api = $"https://avatar.roblox.com/v1/users/{UserId}/avatar";

            var Resp = await Http.GetAsync(Api);
            var JsonData = await Resp.Content.ReadFromJsonAsync<JsonObject>();

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
        public async Task<string> ZipUserIdObjToBuffer(long UserId)
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
        public async Task<JsonObject> ItemDetail(long ItemId)
        {
            var Api = $"https://catalog.roblox.com/v1/catalog/items/{ItemId}/details?itemType=Asset";

            var Resp = await Http.GetAsync(Api);
            var JsonData = await Resp.Content.ReadFromJsonAsync<JsonObject>();

            return JsonData!;
        }
        public async Task<MeshData> GetItemObj(long ItemId)
        {
            // Batch
            var BatchApi = $"https://thumbnails.roblox.com/v1/assets-thumbnail-3d?assetId={ItemId}";
            var BatchResp = await Http.GetAsync(BatchApi);
            var BatchJsonData = await BatchResp.Content.ReadFromJsonAsync<JsonObject>();
            
            // MeshData 
            var MeshResp = await Http.GetAsync(BatchJsonData!["imageUrl"]!.GetValue<string>());
            var MeshJsonData = await MeshResp.Content.ReadFromJsonAsync<JsonObject>();

            var TexUrls = MeshJsonData!["textures"]!.AsArray().Select(sel => GetHashUrl(sel!.GetValue<string>()));
            var TexArr = new TexStruct
            {
                Hash = MeshJsonData["textures"]!.AsArray().Select(sel => sel!.GetValue<string>()).ToArray(),
                Url = TexUrls.ToArray()
            };
            
            var ModelResp = new MeshData
            {
                Obj = GetHashUrl(MeshJsonData["obj"]!.GetValue<string>()),
                Mtl = GetHashUrl(MeshJsonData["mtl"]!.GetValue<string>()),
                Tex = TexArr,
                Pos = new PosStruct
                {
                    X = MeshJsonData["aabb"]!["max"]!["x"]!.GetValue<float>() / -1,
                    Y = MeshJsonData["aabb"]!["max"]!["z"]!.GetValue<float>() / -1,
                    Z = MeshJsonData["aabb"]!["max"]!["y"]!.GetValue<float>() / -1
                }
            };

            return ModelResp;
        }
        public async Task<string> ZipItemObjToBuffer(long ItemId)
        {
            var ZipMemory = new MemoryStream();
            using (var Zip = new ZipArchive(ZipMemory, ZipArchiveMode.Create))
            {
                // get detail
                var ItemData = await ItemDetail(ItemId);

                // get item obj
                var ItemObj = await GetItemObj(ItemId);

                // Mesh Data
                var ObjResp = await Http.GetAsync(ItemObj.Obj);
                var ObjData = await ObjResp.Content.ReadAsStringAsync();

                var MtlResp = await Http.GetAsync(ItemObj.Mtl);
                var MtlDataRaw = await MtlResp.Content.ReadAsStringAsync();

                var UrlsTex = ItemObj.Tex.Url;
                var HashedTex = ItemObj.Tex.Hash;
                var DeHashTex = HashedTex.Select((value, index) =>
                {
                    index++;
                    return $"{ItemId}_Tex{index}.png";
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
                var ZipEntryObj = Zip.CreateEntry($"{ItemId}.obj");
                using (var ZipObjStream = new StreamWriter(ZipEntryObj.Open()))
                {
                    ZipObjStream.Write(ObjData);
                }
                
                var ZipEntryMtl = Zip.CreateEntry($"{ItemId}.mtl");
                using (var ZipMtlStream = new StreamWriter(ZipEntryMtl.Open()))
                {
                    ZipMtlStream.Write(MtlData);
                }

                // pos.txt
                if(ItemObj.Pos != null)
                {
                    var ZipEntryPosTxt = Zip.CreateEntry("pos.txt");
                    using (var ZipPosTxtStream = new StreamWriter(ZipEntryPosTxt.Open()))
                    {
                        var Pos = ItemObj.Pos.GetType().GetProperties().ToDictionary(dict => dict.Name, dict => dict.GetValue(ItemObj.Pos));
                        ZipPosTxtStream.Write(string.Join("\n", Pos.Select(sel => sel.Value)));
                    }
                }
            }

            var ZipBase64 = Convert.ToBase64String(ZipMemory.ToArray());
            return $"data:application/zip;base64,{ZipBase64}";
        }

        // bundle
        public async Task<JsonObject> BundleDetail(long BundleId)
        {
            var Api = $"https://catalog.roblox.com/v1/bundles/details?bundleIds[]={BundleId}";
            
            var Resp = await Http.GetAsync(Api);
            var JsonData = await Resp.Content.ReadFromJsonAsync<JsonArray>();
            if(JsonData!.Count() < 1)
            {
                throw new Exception("Bundle not found!");
            }

            return JsonData![0]!.AsObject();
        }
        public async Task<long> BundleOutfitId(long BundleId)
        {
            var BundleData = await BundleDetail(BundleId);

            long OutfitId = 0;
            var Items = BundleData["items"]!.AsArray();
            foreach (var item in Items) {
                if(item!["type"]!.GetValue<string>() == "UserOutfit" && OutfitId > 0) {}
                {
                    OutfitId = item["id"]!.GetValue<long>();
                }
            }

            return OutfitId;
        }
        public async Task<MeshData> GetBundleObj(long OutfitId)
        {
            // Batch
            var BatchApi = $"https://thumbnails.roblox.com/v1/users/outfit-3d?outfitId={OutfitId}";
            var BatchResp = await Http.GetAsync(BatchApi);
            var BatchJsonData = await BatchResp.Content.ReadFromJsonAsync<JsonObject>();
            
            // MeshData 
            var MeshResp = await Http.GetAsync(BatchJsonData!["imageUrl"]!.GetValue<string>());
            var MeshJsonData = await MeshResp.Content.ReadFromJsonAsync<JsonObject>();

            var TexUrls = MeshJsonData!["textures"]!.AsArray().Select(sel => GetHashUrl(sel!.GetValue<string>()));
            var TexArr = new TexStruct
            {
                Hash = MeshJsonData["textures"]!.AsArray().Select(sel => sel!.GetValue<string>()).ToArray(),
                Url = TexUrls.ToArray()
            };
            
            var ModelResp = new MeshData
            {
                Obj = GetHashUrl(MeshJsonData["obj"]!.GetValue<string>()),
                Mtl = GetHashUrl(MeshJsonData["mtl"]!.GetValue<string>()),
                Tex = TexArr,
                Pos = new PosStruct
                {
                    X = MeshJsonData["aabb"]!["max"]!["x"]!.GetValue<float>() / -1,
                    Y = MeshJsonData["aabb"]!["max"]!["z"]!.GetValue<float>() / -1,
                    Z = MeshJsonData["aabb"]!["max"]!["y"]!.GetValue<float>() / -1
                }
            };

            return ModelResp;
        }
        public async Task<string> ZipBundleObjToBuffer(long BundleId)
        {
            var ZipMemory = new MemoryStream();
            using (var Zip = new ZipArchive(ZipMemory, ZipArchiveMode.Create))
            {
                // get detail
                var OutfitId = await BundleOutfitId(BundleId);

                // get bundle obj
                var BundleObj = await GetBundleObj(OutfitId);

                // Mesh Data
                var ObjResp = await Http.GetAsync(BundleObj.Obj);
                var ObjData = await ObjResp.Content.ReadAsStringAsync();

                var MtlResp = await Http.GetAsync(BundleObj.Mtl);
                var MtlDataRaw = await MtlResp.Content.ReadAsStringAsync();

                var UrlsTex = BundleObj.Tex.Url;
                var HashedTex = BundleObj.Tex.Hash;
                var DeHashTex = HashedTex.Select((value, index) =>
                {
                    index++;
                    return $"{BundleId}_Tex{index}.png";
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
                var ZipEntryObj = Zip.CreateEntry($"{BundleId}.obj");
                using (var ZipObjStream = new StreamWriter(ZipEntryObj.Open()))
                {
                    ZipObjStream.Write(ObjData);
                }
                
                var ZipEntryMtl = Zip.CreateEntry($"{BundleId}.mtl");
                using (var ZipMtlStream = new StreamWriter(ZipEntryMtl.Open()))
                {
                    ZipMtlStream.Write(MtlData);
                }

                // pos.txt
                if(BundleObj.Pos != null)
                {
                    var ZipEntryPosTxt = Zip.CreateEntry("pos.txt");
                    using (var ZipPosTxtStream = new StreamWriter(ZipEntryPosTxt.Open()))
                    {
                        var Pos = BundleObj.Pos.GetType().GetProperties().ToDictionary(dict => dict.Name, dict => dict.GetValue(BundleObj.Pos));
                        ZipPosTxtStream.Write(string.Join("\n", Pos.Select(sel => sel.Value)));
                    }
                }
            }

            var ZipBase64 = Convert.ToBase64String(ZipMemory.ToArray());
            return $"data:application/zip;base64,{ZipBase64}";
        }
    }
}