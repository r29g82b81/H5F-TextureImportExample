
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Formats.Tar;
using System.Runtime.InteropServices;
using DirectXTexNet;
using Infinite_module_test;
using static Infinite_module_test.tag_structs;

using DirectXTexNet;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;


namespace InfiniteTextureCompiler
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //pooper(
            //    "C:\\Users\\ct770\\Downloads\\tag testing\\h1magnum\\materials\\diffuse",
            //    "C:\\Users\\ct770\\Downloads\\tag testing\\h1magnum\\materials\\diffuse_res_0",
            //    "C:\\Users\\ct770\\Downloads\\tag testing\\h1magnum\\materials\\magnum_diffuse.png",
            //    "C:\\Users\\ct770\\Downloads\\tag testing\\h1magnum\\materials\\magnum_diffuse_compiled_LITE"
            //);
            pooper(
                "C:\\Users\\ct770\\Downloads\\tag testing\\h1magnum\\materials\\normal",
                "C:\\Users\\ct770\\Downloads\\tag testing\\h1magnum\\materials\\normal_res_0",
                "C:\\Users\\ct770\\Downloads\\tag testing\\h1magnum\\materials\\magnum_zbump.png",
                "C:\\Users\\ct770\\Downloads\\tag testing\\h1magnum\\materials\\magnum_normal_compiled"
            );
            pooper(
                "C:\\Users\\ct770\\Downloads\\tag testing\\h1magnum\\materials\\controlmap",
                "C:\\Users\\ct770\\Downloads\\tag testing\\h1magnum\\materials\\controlmap_res_0",
                "C:\\Users\\ct770\\Downloads\\tag testing\\h1magnum\\materials\\magnum_control.png",
                "C:\\Users\\ct770\\Downloads\\tag testing\\h1magnum\\materials\\magnum_control_compiled"
            );
        }

        //static void Main(string[] args) { 
        //    Console.WriteLine("Input source image path");
        //    string? input_path = Console.ReadLine();
        //    if (string.IsNullOrEmpty(input_path)) throw new Exception("bad input");

        //    Console.WriteLine("Input destination bitm path");
        //    string? output_path = Console.ReadLine();
        //    if (string.IsNullOrEmpty(output_path)) throw new Exception("bad input");


        //    Console.WriteLine("beginning conversion process...");
        //    convert_image(input_path, output_path);
        //    Console.WriteLine("process complete.");
        //}
        static tag load_tag(string file, string resource){
            if (!File.Exists(file)) throw new Exception("file does not exist");
            byte[] file_bytes = File.ReadAllBytes(file);


            if (!File.Exists(resource)) throw new Exception("resource does not exist");
            byte[] resource_bytes = File.ReadAllBytes(resource);

            var v = new List<KeyValuePair<byte[], bool>>();

            v.Add(new(resource_bytes, true));
            tag test = new tag(v);
            if (!test.Load_tag_file(file_bytes)) throw new Exception("failed to load tag");
            return test;
        }

        static void convert_image(string tag_file, string tag_res_file,  string image_path, string output_path){
            //System.Drawing.Image image1 = System.Drawing.Image.FromFile(image_path);
            //var v = image1.PixelFormat;
            //var pixels = image1.;


           

            Bitmap bmp = new Bitmap(image_path);
            //Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData rawOriginal = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            int origByteCount = rawOriginal.Stride * rawOriginal.Height;
            byte[] origBytes = new byte[origByteCount];
            System.Runtime.InteropServices.Marshal.Copy(rawOriginal.Scan0, origBytes, 0, origByteCount);


            // we now copy over this information to the loaded tag
            tag bitmap_tag = load_tag(tag_file, tag_res_file);
            bitmap_tag.set_basic("force bitmap format#overrides the format defined by usage", [0,0]);
            bitmap_tag.set_basic("bitmaps*\\0\\format*#DO NOT CHANGE", [11, 0]);

            bitmap_tag.set_number("bitmaps*\\0\\width*:pixels#DO NOT CHANGE", bmp.Width.ToString());
            bitmap_tag.set_number("bitmaps*\\0\\height*:pixels#DO NOT CHANGE", bmp.Height.ToString());
            bitmap_tag.set_basic("bitmaps*\\0\\flags*", [1]);
            bitmap_tag.set_number("bitmaps*\\0\\mipmap count*#DO NOT CHANGE (not counting the highest resolution)", "0");
            bitmap_tag.set_datablock("bitmaps*\\0\\bitmap resource handle*\\\\pixels*", origBytes);


            var streaming_tagblock = bitmap_tag.get_tagblock("bitmaps*\\0\\bitmap resource handle*\\\\streamingData*");
            streaming_tagblock.blocks.Clear();


            tag.compiled_tag output = bitmap_tag.compile();
            // we aren't going to output resources with this, so we only need to worry about spitting out the main file
            File.WriteAllBytes(output_path, output.tag_bytes);
            for (int i = 0; i < output.resource_bytes.Count; i++)
            {
                File.WriteAllBytes($"{output_path}_res_{i}", output.resource_bytes[i]);
            }
        }



        static void pooper(string tag_file, string tag_res_file, string image_path, string output_path){

            Bitmap bmp = new Bitmap(image_path);
            BitmapData bitmapData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            try{
                // Copy source pixels
                int byteCount = bitmapData.Stride * bitmapData.Height;
                byte[] rgbaBytes = new byte[byteCount];
                Marshal.Copy(bitmapData.Scan0, rgbaBytes, 0, byteCount);

                // Create DirectXTex image
                ScratchImage source = TexHelper.Instance.Initialize2D(DXGI_FORMAT.B8G8R8A8_UNORM, bmp.Width, bmp.Height, 1, 1, CP_FLAGS.NONE);
                DirectXTexNet.Image image = source.GetImage(0, 0, 0);

                Marshal.Copy(rgbaBytes, 0, image.Pixels, rgbaBytes.Length);
                
                // Generate full mip chain
                //ScratchImage mipChain = source.GenerateMipMaps(TEX_FILTER_FLAGS.DEFAULT, 0);

                // Compress to BC3
                ScratchImage compressed = source.Compress(DXGI_FORMAT.BC3_UNORM, TEX_COMPRESS_FLAGS.DEFAULT, 1.0f);
                TexMetadata meta = compressed.GetMetadata();

                // Flatten every mip into one contiguous buffer

                using MemoryStream ms = new MemoryStream();
                //for (int mip = 0; mip < (int)meta.MipLevels; mip++){
                //    DirectXTexNet.Image mipImage = compressed.GetImage(mip, 0, 0);
                //    byte[] mipBytes = new byte[(int)mipImage.SlicePitch];
                //    Marshal.Copy(mipImage.Pixels, mipBytes, 0, mipBytes.Length);
                //    ms.Write(mipBytes, 0, mipBytes.Length);
                //}
                //ms.Write(mipBytes, 0, mipBytes.Length);
                //byte[] textureBytes = ms.ToArray();

                DirectXTexNet.Image mipImage = compressed.GetImage(0, 0, 0);
                byte[] mipBytes = new byte[(int)mipImage.SlicePitch];
                Marshal.Copy(mipImage.Pixels, mipBytes, 0, mipBytes.Length);


                tag bitmap_tag = load_tag(tag_file, tag_res_file);
                bitmap_tag.set_basic("force bitmap format#overrides the format defined by usage", new byte[] { 0, 0 });
                bitmap_tag.set_number("max mipmap level#0 = use default defined by usage", "1");
                bitmap_tag.set_basic("bitmaps*\\0\\format*#DO NOT CHANGE", new byte[] { 16, 0 });
                bitmap_tag.set_number("bitmaps*\\0\\width*:pixels#DO NOT CHANGE", bmp.Width.ToString());
                bitmap_tag.set_number("bitmaps*\\0\\height*:pixels#DO NOT CHANGE", bmp.Height.ToString());
                bitmap_tag.set_basic("bitmaps*\\0\\flags*", new byte[] { 3 });
                bitmap_tag.set_number("bitmaps*\\0\\mipmap count*#DO NOT CHANGE (not counting the highest resolution)", "0");
                bitmap_tag.set_datablock("bitmaps*\\0\\bitmap resource handle*\\\\pixels*", mipBytes);

                var streaming_tagblock = bitmap_tag.get_tagblock( "bitmaps*\\0\\bitmap resource handle*\\\\streamingData*");
                streaming_tagblock.blocks.Clear();

                tag.compiled_tag output = bitmap_tag.compile();
                File.WriteAllBytes( output_path, output.tag_bytes);
                for (int i = 0; i < output.resource_bytes.Count; i++)
                    File.WriteAllBytes($"{output_path}_res_{i}",output.resource_bytes[i]);
            }
            finally
            {
                bmp.UnlockBits(bitmapData);
                bmp.Dispose();
            }

        }

    }
}