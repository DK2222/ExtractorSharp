﻿using System.Drawing;
using System.IO;
using ExtractorSharp.Core.Lib;
using ExtractorSharp.Handle;
using ExtractorSharp.Json.Attr;

namespace ExtractorSharp.Data {

    public class Sprite {
        /// <summary>
        /// 色位
        /// </summary>
        /// 
        public ColorBits Type { set; get; } = ColorBits.ARGB_1555;
        /// <summary>
        /// 贴图内容
        /// </summary>
        [LSIgnore]
        public Bitmap Picture {
            get {
                if (Type == ColorBits.LINK) {
                    return Target.Picture;
                }
                if (IsOpen) {//如果已打开,直接返回image
                    return _image;
                }
                return _image = Parent.ConvertToBitmap(this);//使用父容器解析
            }
            set {
                _image = value;
                if (value != null) {
                    Size = value.Size;
                }
            }
        }

        [LSIgnore]
        public bool IsOpen => _image != null;


        [LSIgnore]
        private Bitmap _image;
        /// <summary>
        /// 贴图宽高
        /// </summary>
        /// 
        [LSIgnore]
        public Size Size = new Size(1, 1);


        /// <summary>
        /// 贴图坐标    
        /// </summary>
        [LSIgnore]
        public Point Location;

        public int X {
            set => Location.X = value;
            get => Location.X;
        }

        public int Y {
            set => Location.Y = value;
            get => Location.Y;
        }

        public int Width {
            set => Size.Width = value;
            get => Size.Width;

        }

        public int Height {
            set => Size.Height = value;
            get => Size.Height;
        }

        /// <summary>
        /// 帧域宽高
        /// </summary>
        [LSIgnore]
        public Size Canvas_Size = Size.Empty;

        public int Canvas_Width {
            set => Canvas_Size = new Size(value, Canvas_Height);
            get => Canvas_Size.Width;
        }

        public int Canvas_Height {
            set => Canvas_Size = new Size(Canvas_Width, value);
            get => Canvas_Size.Height;
        }

        /// <summary>
        /// 压缩类型
        /// </summary>
        public Compress Compress = Compress.NONE;


        /// <summary>
        /// 数据长度
        /// </summary>
        [LSIgnore]
        public int Length = 2;

        /// <summary>
        /// 当贴图为链接贴图时所指向的贴图
        /// </summary>
        [LSIgnore]
        public Sprite Target;

        /// <summary>
        /// 贴图在V2,V4时的数据
        /// </summary>
        [LSIgnore]
        public byte[] Data = new byte[2];

        /// <summary>
        /// 贴图在img中的下标
        /// </summary>
        public int Index;

        /// <summary>
        /// 存储该贴图的img
        /// </summary>
        [LSIgnore]
        public Album Parent;

        public Sprite() { }

        public Sprite(Album Parent) => this.Parent = Parent;

        /// <summary>
        /// 文件版本
        /// </summary>
        [LSIgnore]
        public Img_Version Version => Parent.Version;

        [LSIgnore]
        public bool Hidden => Width * Height == 1 && Compress == Compress.NONE;


        public void Load() {
            _image = Parent.ConvertToBitmap(this);//使用父容器
        }

        /// <summary>
        /// 替换贴图
        /// </summary>
        /// <param name="type"></param>
        /// <param name="isAdjust"></param>
        /// <param name="bmp"></param>
        public void ReplaceImage(ColorBits type, bool isAdjust, Bitmap bmp) {
            if (bmp == null) {
                return;
            }
            Picture = bmp;
            Target = null;
            Type = type == ColorBits.UNKOWN ? Type : type;
            if (type == ColorBits.UNKOWN) {
                if (Type == ColorBits.LINK) {
                    type = ColorBits.ARGB_1555;
                } else if (Version != Img_Version.Ver5 && Type > ColorBits.LINK) {
                    type = Type - 4;
                } else {
                    type = Type;
                }
            }
            Type = type;
            if (isAdjust) {
                X += bmp.Width - Size.Width;
                Y += bmp.Height - Size.Height;
            }
            Size = bmp.Size;
            if (Canvas_Height < bmp.Height) {
                Canvas_Height = bmp.Height;
            }
            if (Canvas_Width < bmp.Width) {
                Canvas_Width = bmp.Width;
            }
            if (Width * Height > 1) {
                Compress = Compress.ZLIB;
            }
        }


        /// <summary>
        /// 去画布化
        /// </summary>
        public void UnCanvasImage() {
            if (Type == ColorBits.LINK || Compress == Compress.NONE) {
                return;
            }
            if (Picture == null) {
                return;
            }
            var rct = Picture.Scan();
            var image = new Bitmap(rct.Width, rct.Height);
            var g = Graphics.FromImage(image);
            var empty = new Rectangle(Point.Empty, rct.Size);
            g.DrawImage(Picture, empty, rct, GraphicsUnit.Pixel);
            g.Dispose();
            Size = rct.Size;
            Location = Location.Add(rct.Location);
            Picture = image;
        }

        /// <summary>
        /// 画布化
        /// </summary>
        /// <param name="Target"></param>
        public void CanvasImage(Size Target) {
            if (Type == ColorBits.LINK) {
                return;
            }
            Picture = Picture.Canvas(new Rectangle(Location, Target));
            Size = Target;
            Location = Point.Empty;
        }



        /// <summary>
        /// 数据校正
        /// </summary>
        public virtual void Adjust() {
            if (Type == ColorBits.LINK) {
                Length = 0;
                return;
            }
            if (!IsOpen)
                return;
            Data = Parent.ConvertToByte(this);
            if (Data.Length > 0 && Compress >= Compress.ZLIB) {
                Data = Zlib.Compress(Data);
            }
            Length = Data.Length;            //不压缩时，按原长度保存
        }


        public bool Equals(Sprite entity) => entity != null && Parent.Equals(entity.Parent) && Index == entity.Index;

        public override string ToString() {
            if (Type == ColorBits.LINK && Target != null)
                return Index + "," + Language.Default["TargetIndex"] + Target.Index;
            return Index + "," + Type + "," + Language.Default["Position"] + "(" + Location.GetString() + ")," + Language.Default["Size"] + "(" + Size.GetString() + ")," + Language.Default["CanvasSize"] + "(" + Canvas_Size.GetString() + ")";
        }

        public Sprite Clone(Album album) {
            return new Sprite(album) {
                Picture = Picture,
                Compress = Compress,
                Type = Type,
                Location = Location,
                Canvas_Size = Canvas_Size,
                Target = Target
            };
        }


    }

    /// <summary>
    /// 色位
    /// </summary>
    public enum ColorBits {
        ARGB_1555 = 0x0e,
        ARGB_4444 = 0x0f,
        ARGB_8888 = 0x10,
        LINK = 0x11,
        DXT_1 = 0x12,
        DXT_3 = 0x13,
        DXT_5 = 0x14,
        UNKOWN = 0x00,
    }
    /// <summary>
    /// 压缩类型
    /// </summary>
    public enum Compress {
        ZLIB = 0x06,
        NONE = 0x05,
        DDS_ZLIB = 0x07,
        UNKNOW = 0x01
    }

}
