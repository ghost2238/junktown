﻿using FOCommon.Graphic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fomap
{
    class FOnlineProto
    {
        public int pid;
        public string picMap;
    }

    enum MapObjectType
    {
        Critter,
        Item,
        Scenery
    }

    class MapObject
    {
        public int type;
        public int pid;
        public int x;
        public int y;
    }

    class FRMData
    {
        public int gfx; // index to gfx array.
        public int width;
        public int height;
        public int shiftX;
        public int shiftY;
    }

    class Program
    {
        static string ParseMapVal(string line)
        {
            var spl = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return spl[1];
        }

        static void Main(string[] args)
        {
            var mapDir = @"D:\Fallout\maps\";

            List<string> gfx = new List<string>();
            List<string> load = new List<string>();
            List<string> tiles = new List<string>();
            List<string> roofs = new List<string>();
            List<string> scenery = new List<string>();
            List<FOnlineProto> protos = new List<FOnlineProto>();
            List<MapObject> mapobject = new List<MapObject>();
            List<FRMData> frmData = new List<FRMData>();
            var gfxDict = new Dictionary<int, int>();

            var loadMap = "ul_junkyard.fomap"; 

            foreach (var proto in Directory.GetFiles(@"C:\Users\Markus\Documents\GitHub\fo2238\Server\proto\items", "*.fopro"))
            {
                int pid = 0;
                string picMap = "";
                foreach(var line in File.ReadAllLines(proto))
                {
                    if (line.StartsWith("ProtoId="))
                        pid = int.Parse(line.Split('=')[1]);
                    if (line.StartsWith("PicMap="))
                    {
                        picMap = line.Split('=')[1].Replace(@"art\","").Replace(@"\","/");
                        protos.Add(new FOnlineProto
                        {           
                            pid = pid,
                            picMap = picMap
                        });
                    }
                }
            }


            bool parsingObjects = false;
            int containerUID = 0;
            MapObjectType mapType = 0;
            int mapPid = 0;
            int mapX = 0;
            int mapY = 0;
            foreach (var line in File.ReadAllLines(@"D:\Fallout\maps\" + loadMap))
            {
                if (line.StartsWith("tile") || line.StartsWith("roof"))
                {
                    if (line.StartsWith("tile_l"))
                    {
                        continue;
                    }
    
                    var spl = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    var img = spl[3].Replace(@"art\tiles\", "tiles/").ToUpper().Replace(".FRM", ".png").Replace("TILES", "tiles");
                    var idx = gfx.IndexOf(img);
                    if (idx == -1)
                    {
                        gfx.Add(img);
                        idx = gfx.Count - 1;
                        load.Add($"'{img}'");
                        if (!File.Exists(mapDir + img))
                        {
                            var falloutFRM = FalloutFRMLoader.LoadFRM(File.ReadAllBytes(mapDir + img.Replace(".png", ".frm")), Color.FromArgb(11, 0, 11));
                            var c = new Bitmap(falloutFRM.Frames[0]);
                            c.MakeTransparent(Color.FromArgb(11, 0, 11));
                            c.Save(mapDir + img, ImageFormat.Png);
                        }
                    }
                    var x = int.Parse(spl[1]);
                    var y = int.Parse(spl[2]);

                    if (line.StartsWith("tile"))
                    {
                        tiles.Add(idx.ToString());
                        tiles.Add(x.ToString());
                        tiles.Add(y.ToString());
                    }
                    else if(line.StartsWith("roof"))
                    {
                        roofs.Add(idx.ToString());
                        roofs.Add(x.ToString());
                        roofs.Add(y.ToString());
                    }
                }

                if (line.StartsWith("[Objects]"))
                    parsingObjects = true;

                if (parsingObjects)
                {
                    if (line.StartsWith("MapObjType"))
                        mapType = (MapObjectType)int.Parse(ParseMapVal(line));
                    if (line.StartsWith("ProtoId"))
                        mapPid = int.Parse(ParseMapVal(line));
                    if (line.StartsWith("MapX"))
                        mapX = int.Parse(ParseMapVal(line));
                    if (line.StartsWith("MapY"))
                        mapY = int.Parse(ParseMapVal(line));
                    if (line.StartsWith("ContainerUID"))
                        containerUID = int.Parse(ParseMapVal(line));

                    if (line == "")
                    {
                        if (mapType == MapObjectType.Scenery || mapType == MapObjectType.Item)
                        {
                            // items in containers, don't render.
                            if (mapType == MapObjectType.Scenery || (mapType == MapObjectType.Item && containerUID == 0))
                            {
                                mapobject.Add(new MapObject
                                {
                                    pid = mapPid,
                                    type = (int)mapType,
                                    x = mapX,
                                    y = mapY
                                });
                            }
                            containerUID = 0;
                            mapType = 0;
                            mapPid = 0;
                            mapX = 0;
                            mapY = 0;
                        }
                    }
                }
            }

            /*protos.Add(new FOnlineProto
            {
                pid = 1207,
                picMap = "scenery/fo-rotators.png"
            });

            mapobject.Add(new MapObject()
            {
                pid = 1207,
                type = 2,
                x = 119,
                y = 236,
            });*/

            var mapObjList = new List<string>();
            foreach (var m in mapobject.OrderBy(m => m.x + m.y * 2))
            {
                var proto = protos.Where(x => x.pid == m.pid).FirstOrDefault();
                var idx = gfx.IndexOf(proto.picMap);

                if (proto.picMap.Contains("fofrm"))
                    continue;

                var frmIdx = 0;
                if (idx == -1)
                {
                    
                    gfx.Add(proto.picMap);
                    load.Add($"'{proto.picMap.Replace("frm", "png")}'");
                    if (proto.picMap.Contains(".png"))
                    {
                        int shiftY = 0;
                        int shiftX = 0;
                        /*if (proto.picMap.Contains("rotators"))
                        {
                            shiftY = -25;
                            shiftX = 8;
                        }*/

                        Image img = Image.FromFile(mapDir + proto.picMap);
                        var data = new FRMData
                        {
                            gfx = gfx.Count - 1,
                            height = img.Height,
                            width = img.Width,
                            shiftX = shiftX,
                            shiftY = shiftY
                        };
                        frmData.Add(data);
                        gfxDict[gfx.Count - 1] = frmData.Count - 1;
                        frmIdx = frmData.Count - 1;
                    }
                    else
                    {
                        // load frmData
                        var file = mapDir + proto.picMap;
                        FalloutFRM falloutFRM = null;
                        try
                        {
                            falloutFRM = FalloutFRMLoader.LoadFRM(File.ReadAllBytes(file), Color.FromArgb(11, 0, 11));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Unable to open {file}: {ex.Message}");
                        }

                        // No png on disk? save it...
                        var pngPath = (mapDir + proto.picMap).Replace("frm", "png");
                        if (!File.Exists(pngPath))
                        {
                            var c = new Bitmap(falloutFRM.Frames[0]);
                            c.MakeTransparent(Color.FromArgb(11, 0, 11));
                            c.Save(pngPath, ImageFormat.Png);
                        }


                        var shift = falloutFRM.PixelShift;
                        var data = new FRMData
                        {
                            gfx = gfx.Count - 1,
                            height = falloutFRM.Frames[0].Height,
                            width = falloutFRM.Frames[0].Width,
                            shiftX = shift.X,
                            shiftY = shift.Y
                        };
                        frmData.Add(data);
                        gfxDict[gfx.Count - 1] = frmData.Count - 1;
                        frmIdx = frmData.Count - 1;
                    }
                }
                else
                {
                    frmIdx = gfxDict[idx];
                }
                mapObjList.Add(frmIdx.ToString());
                mapObjList.Add(m.x.ToString());
                mapObjList.Add(m.y.ToString());
            }
            var frm = new List<string>();
            foreach(var f in frmData)
            {
                frm.Add(f.gfx.ToString());
                frm.Add(f.height.ToString());
                frm.Add(f.width.ToString());
                frm.Add(f.shiftX.ToString());
                frm.Add(f.shiftY.ToString());
            }

            var mapNameNoExt = loadMap.Split('.')[0];

            var template = File.ReadAllText(@"C:\Users\Markus\Documents\GitHub\junktown\fomap\template.html");
            template = template.Replace("[LOAD_CODE]", "var images = [" + string.Join(",", load) + "];");
            template = template.Replace("[TILES]", "var tiles = [" + string.Join(",", tiles) + "];");
            template = template.Replace("[ROOFS]", "var roofs = [" + string.Join(",", roofs) + "];");
            //template = template.Replace("[ROOFS]", "var roofs = [];");
            template = template.Replace("[FRM_DATA]", "var frmData = [" + string.Join(",", frm) + "];");
            template = template.Replace("[MAP_OBJECTS]", "var mapObj = [" + string.Join(",", mapObjList) + "];");
            //template = template.Replace("[MAP_OBJECTS]", "var mapObj = [];");
            template = template.Replace("[MAP_NAME]", loadMap);
            File.WriteAllText(@"D:\Fallout\maps\"+mapNameNoExt+".html", template);
        }
    }
}
