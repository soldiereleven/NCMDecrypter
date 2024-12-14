using System.Formats.Tar;
using System.Security.Cryptography;
using System.Text.Json;
using TagLib;

namespace NCMDecrypter
{
    public class Decrypter
    {
        private IEnumerable<byte> _data;
        private MusicMetaInfo _metaInfo;
        private int _currentbyte = 0;
        private byte[] _cover;
        private static readonly byte[] MagicHeader = { 0x43, 0x54, 0x45, 0x4e, 0x46, 0x44, 0x41, 0x4d,0x01,0x70 };
        private static readonly byte[] CoreKey = { 0x68, 0x7A, 0x48, 0x52, 0x41, 0x6D, 0x73, 0x6F, 0x35, 0x6B, 0x49, 0x6E, 0x62, 0x61, 0x78, 0x57 };
        private static readonly byte[] MetaKey = { 0x23, 0x31, 0x34, 0x6C, 0x6A, 0x6B, 0x5F, 0x21, 0x5C, 0x5D, 0x26, 0x30, 0x55, 0x3C, 0x27, 0x28 };
        public Decrypter(IEnumerable<byte> data)
        {
            _data = data;
        }
        public Decrypter(string filePath)
        {
            _data=System.IO.File.ReadAllBytes(filePath);
        }
        public Decrypter(MemoryStream ms)
        {
            byte[] buffer=new byte[ms.Length];
            ms.Read(buffer, 0, buffer.Length);
            _data= buffer;
            ms.Close();
        }
        public async Task Execute(DecryptParam param)
        {
            param.OutputName = Path.GetFileNameWithoutExtension(param.OutputName);
            //写入文件
            await Decrypt(param);

            if (param.FixMetadata == true)
            {
                await FixMetadata(param);
            }
        }
        
        private async Task FixMetadata(DecryptParam param)
        {
            var tfile = TagLib.File.Create(param.OutputPath);
            tfile.Tag.Title = _metaInfo.musicName;
            tfile.Tag.Album = _metaInfo.album;
            string[] artists=new string[_metaInfo.artist.Count()];
            foreach(var artist in _metaInfo.artist)
            {
                artist.Add(artist[0]);
            }
            tfile.Tag.Performers = artists;
            if (_metaInfo.alias.Count()>0)
            {
                tfile.Tag.Subtitle= _metaInfo.alias[0];
            }else if(_metaInfo.transNames.Count()>0)
            {
                tfile.Tag.Subtitle = _metaInfo.transNames[0];
            }
            tfile.Tag.AlbumArtists = artists;
            tfile.Tag.Pictures =
            [
                new Picture(new ByteVector(_cover,_cover.Length))
            ];
            tfile.Save();
        }
        private async Task Decrypt(DecryptParam param)
        {
            _currentbyte = 0;
            //验证头
            byte[] header = getBytes(10);
            if (!header.SequenceEqual(MagicHeader)) {
                //不是有效ncm文件
                throw new FormatException("Target file is not a valid NCM(Netease Copyright Music) file!");
            }
            //获取密钥长度
            byte[] keyLenHex = getBytes(4);
            int keyLen = getLen(keyLenHex);
            //读取密钥
            byte[] keyRaw= getBytes(keyLen);
            for(int i = 0; i < 128; i++)
            {
                keyRaw[i] ^= 0x64;
            }
            string decryptedKey;
            //AES解密密钥
            using (Aes aes = Aes.Create())
            {
                aes.Key= CoreKey;
                aes.Mode= CipherMode.ECB;
                aes.Padding = PaddingMode.PKCS7;
                ICryptoTransform decrypter= aes.CreateDecryptor();
                byte[] encryptedByte = keyRaw;
                using (MemoryStream memoryStream = new MemoryStream(encryptedByte))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream,decrypter,CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader(cryptoStream))
                        {
                            decryptedKey =streamReader.ReadToEnd().ToString();
                        }
                    }
                }
            }
            decryptedKey = decryptedKey.Substring(17);
            //获取密钥长度
            keyLen=decryptedKey.Length;
            //将密钥转为byte数组
            byte[] keyData=System.Text.Encoding.UTF8.GetBytes(decryptedKey);
            //RC4
            //生成S盒
            byte[] keyBox = new byte[256];
            for(int i = 0; i < 256; i++) keyBox[i] = (byte)i;
            int c = 0;
            int lastByte = 0;
            int keyOffset = 0;
            for(int i = 0; i < 256; i++)
            {
                byte swap=keyBox[i];
                c = (swap + lastByte + keyData[keyOffset]) & 0xff;
                keyOffset += 1;
                if(keyOffset>=keyLen)keyOffset = 0;
                keyBox[i] = keyBox[c];
                keyBox[c] = swap;
                lastByte = c;
            }
            //读取meta
            byte[] metaLenHex = getBytes(4);
            int metaLen = getLen(metaLenHex);
            byte[] metaRaw=getBytes(metaLen);
            for(int i = 0;i<metaLen;i++)
            {
                metaRaw[i] ^= 0x63;
            }
            metaRaw = metaRaw.Skip(22).ToArray();
            string meta = System.Text.Encoding.UTF8.GetString(metaRaw);
            metaRaw = Convert.FromBase64String(meta);
            string decryptedMeta;
            using (Aes aes = Aes.Create())
            {
                aes.Key = MetaKey;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.PKCS7;
                ICryptoTransform decrypter = aes.CreateDecryptor();
                byte[] encryptedByte = metaRaw;
                using (MemoryStream memoryStream = new MemoryStream(encryptedByte))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decrypter, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader(cryptoStream))
                        {
                            decryptedMeta = streamReader.ReadToEnd().ToString();
                        }
                    }
                }
            }
            decryptedMeta = decryptedMeta.Substring(6);
            MusicMetaInfo? metaInfo=JsonSerializer.Deserialize<MusicMetaInfo>(decryptedMeta);
            if(metaInfo != null)
            {
                _metaInfo = metaInfo;
            }
            else
            {
                throw new FormatException("Broken NCM file: Could not read info information");
            }
            //crc校验码，貌似没用
            byte[] crcCodeRaw = getBytes(4);
            int _crcCode=getLen(crcCodeRaw);
            //5字节的垃圾数据
            byte[] _junk = getBytes(5);
            //读取封面
            byte[] coverLenHex = getBytes(4);
            int coverLen = getLen(coverLenHex);
            byte[] cover = getBytes(coverLen);
            _cover = cover;
            //using (MemoryStream memoryStream = new MemoryStream(cover)) {
            //    FileStream fileStream = new FileStream("E:/1.jpg",FileMode.OpenOrCreate, FileAccess.ReadWrite);
            //    memoryStream.WriteTo(fileStream);
            //    memoryStream.Close();
            //    fileStream.Close();
            //}
            param.OutputPath = Path.Combine(param.OutputPath, param.OutputName + "." + _metaInfo.format);
            using (FileStream fs=new FileStream(param.OutputPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                using(BinaryWriter bw=new BinaryWriter(fs))
                {
                    while (true)
                    {
                        byte[] chunk = getBytes(0x8000);
                        if (chunk.Length == 0) break;
                        for (int i = 1; i < chunk.Length + 1; i++)
                        {
                            int j = i & 0xff;
                            chunk[i - 1] ^= keyBox[(keyBox[j] + keyBox[(keyBox[j] + j) & 0xff]) & 0xff];
                        }
                        bw.Write(chunk);
                    }
                }
            }
            
        }
        private byte[] getBytes(int n)
        {
            var dat = _data.Skip(_currentbyte).Take(n).ToArray();
            _currentbyte += n;
            return dat;
        }
        private int getLen(byte[] bytes)
        {
            string hex="";
            for(int i = 0; i < 4; i++)
            {
                string singleHex = bytes[i].ToString("X2");
                hex = singleHex + hex;
            }
            return Convert.ToInt32(hex,16);
        }
    }
}
