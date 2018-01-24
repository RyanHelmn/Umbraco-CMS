﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Web;

namespace Umbraco.Core.PropertyEditors.ValueConverters // fixme MOVE TO MODELS OR SOMETHING
{
    /// <summary>
    /// Represents a value of the image cropper value editor.
    /// </summary>
    public class ImageCropperValue : IHtmlString, IEquatable<ImageCropperValue>
    {
        /// <summary>
        /// Gets or sets the value source image.
        /// </summary>
        [DataMember(Name="src")]
        public string Src { get; set;}

        /// <summary>
        /// Gets or sets the value focal point.
        /// </summary>
        [DataMember(Name = "focalPoint")]
        public ImageCropperFocalPoint FocalPoint { get; set; }

        /// <summary>
        /// Gets or sets the value crops.
        /// </summary>
        [DataMember(Name = "crops")]
        public IEnumerable<ImageCropperCrop> Crops { get; set; }

        /// <inheritdoc />
        public string ToHtmlString() => Src;

        /// <summary>
        /// Gets a crop.
        /// </summary>
        public ImageCropperCrop GetCrop(string alias)
        {
            if (Crops == null || !Crops.Any())
                return null;

            return string.IsNullOrWhiteSpace(alias)
                ? Crops.First()
                : Crops.FirstOrDefault(x => x.Alias.InvariantEquals(alias));
        }

        // fixme was defined in web project, extension methods?
        internal void AppendCropBaseUrl(StringBuilder url, ImageCropperCrop crop, bool preferFocalPoint)
        {
            if (preferFocalPoint && HasFocalPoint()
                || crop != null && crop.Coordinates == null && HasFocalPoint()
                || crop == null && HasFocalPoint())
            {
                url.Append("?center=");
                url.Append(FocalPoint.Top.ToString(CultureInfo.InvariantCulture));
                url.Append(",");
                url.Append(FocalPoint.Left.ToString(CultureInfo.InvariantCulture));
                url.Append("&mode=crop");
            }
            else if (crop != null && crop.Coordinates != null && preferFocalPoint == false)
            {
                url.Append("?crop=");
                url.Append(crop.Coordinates.X1.ToString(CultureInfo.InvariantCulture)).Append(",");
                url.Append(crop.Coordinates.Y1.ToString(CultureInfo.InvariantCulture)).Append(",");
                url.Append(crop.Coordinates.X2.ToString(CultureInfo.InvariantCulture)).Append(",");
                url.Append(crop.Coordinates.Y2.ToString(CultureInfo.InvariantCulture));
                url.Append("&cropmode=percentage");
            }
            else
            {
                url.Append("?anchor=center");
                url.Append("&mode=crop");
            }
        }

        /// <summary>
        /// Gets the value image url for a specified crop.
        /// </summary>
        public string GetCropUrl(string alias, bool useCropDimensions = true, bool useFocalPoint = false, string cacheBusterValue = null)
        {
            var crop = GetCrop(alias);

            // could not find a crop with the specified, non-empty, alias
            if (crop == null && !string.IsNullOrWhiteSpace(alias))
                return null;

            var url = new StringBuilder();

            AppendCropBaseUrl(url, crop, useFocalPoint);

            if (crop != null && useCropDimensions)
            {
                url.Append("&width=").Append(crop.Width);
                url.Append("&height=").Append(crop.Height);
            }

            if (cacheBusterValue != null)
                url.Append("&rnd=").Append(cacheBusterValue);

            return url.ToString();
        }

        /// <summary>
        /// Gets the value image url for a specific width and height.
        /// </summary>
        public string GetCropUrl(int width, int height, bool useFocalPoint = false, string cacheBusterValue = null)
        {
            var url = new StringBuilder();

            AppendCropBaseUrl(url, null, useFocalPoint);

            url.Append("&width=").Append(width);
            url.Append("&height=").Append(height);

            if (cacheBusterValue != null)
                url.Append("&rnd=").Append(cacheBusterValue);

            return url.ToString();
        }

        /// <summary>
        /// Determines whether the value has a focal point.
        /// </summary>
        /// <returns></returns>
        public bool HasFocalPoint()
            => FocalPoint != null && FocalPoint.Left != 0.5m && FocalPoint.Top != 0.5m;

        /// <summary>
        /// Determines whether the value has a specified crop.
        /// </summary>
        public bool HasCrop(string alias)
            => Crops.Any(x => x.Alias == alias);

        /// <summary>
        /// Determines whether the value has a source image.
        /// </summary>
        public bool HasImage()
            => !string.IsNullOrWhiteSpace(Src);

        #region IEquatable

        /// <inheritdoc />
        public bool Equals(ImageCropperValue other)
            => other != null && (ReferenceEquals(this, other) || Equals(this, other));

        /// <inheritdoc />
        public override bool Equals(object obj)
            => obj != null && (ReferenceEquals(this, obj) || obj is ImageCropperValue other && Equals(this, other));

        private static bool Equals(ImageCropperValue left, ImageCropperValue right)
            => string.Equals(left.Src, right.Src)
               && Equals(left.FocalPoint, right.FocalPoint)
               && left.ComparableCrops.SequenceEqual(right.ComparableCrops);

        private IEnumerable<ImageCropperCrop> ComparableCrops
            => Crops?.OrderBy(x => x.Alias) ?? Enumerable.Empty<ImageCropperCrop>();

        public static bool operator ==(ImageCropperValue left, ImageCropperValue right)
            => Equals(left, right);

        public static bool operator !=(ImageCropperValue left, ImageCropperValue right)
            => !Equals(left, right);

        public override int GetHashCode()
        {
            unchecked
            {
                // properties are, practically, readonly
                // ReSharper disable NonReadonlyMemberInGetHashCode
                var hashCode = Src?.GetHashCode() ?? 0;
                hashCode = (hashCode*397) ^ (FocalPoint?.GetHashCode() ?? 0);
                hashCode = (hashCode*397) ^ (Crops?.GetHashCode() ?? 0);
                return hashCode;
                // ReSharper restore NonReadonlyMemberInGetHashCode
            }
        }

        #endregion

        [DataContract(Name = "imageCropFocalPoint")]
        public class ImageCropperFocalPoint : IEquatable<ImageCropperFocalPoint>
        {
            [DataMember(Name = "left")]
            public decimal Left { get; set; }

            [DataMember(Name = "top")]
            public decimal Top { get; set; }

            #region IEquatable

            /// <inheritdoc />
            public bool Equals(ImageCropperFocalPoint other)
                => other != null && (ReferenceEquals(this, other) || Equals(this, other));

            /// <inheritdoc />
            public override bool Equals(object obj)
                => obj != null && (ReferenceEquals(this, obj) || obj is ImageCropperFocalPoint other && Equals(this, other));

            private static bool Equals(ImageCropperFocalPoint left, ImageCropperFocalPoint right)
                => left.Left == right.Left
                   && left.Top == right.Top;

            public static bool operator ==(ImageCropperFocalPoint left, ImageCropperFocalPoint right)
                => Equals(left, right);

            public static bool operator !=(ImageCropperFocalPoint left, ImageCropperFocalPoint right)
                => !Equals(left, right);

            public override int GetHashCode()
            {
                unchecked
                {
                    // properties are, practically, readonly
                    // ReSharper disable NonReadonlyMemberInGetHashCode
                    return (Left.GetHashCode()*397) ^ Top.GetHashCode();
                    // ReSharper restore NonReadonlyMemberInGetHashCode
                }
            }

            #endregion
        }

        [DataContract(Name = "imageCropData")]
        public class ImageCropperCrop : IEquatable<ImageCropperCrop>
        {
            [DataMember(Name = "alias")]
            public string Alias { get; set; }

            [DataMember(Name = "width")]
            public int Width { get; set; }

            [DataMember(Name = "height")]
            public int Height { get; set; }

            [DataMember(Name = "coordinates")]
            public ImageCropperCropCoordinates Coordinates { get; set; }

            #region IEquatable

            /// <inheritdoc />
            public bool Equals(ImageCropperCrop other)
                => other != null && (ReferenceEquals(this, other) || Equals(this, other));

            /// <inheritdoc />
            public override bool Equals(object obj)
                => obj != null && (ReferenceEquals(this, obj) || obj is ImageCropperCrop other && Equals(this, other));

            private static bool Equals(ImageCropperCrop left, ImageCropperCrop right)
                => string.Equals(left.Alias, right.Alias)
                   && left.Width == right.Width
                   && left.Height == right.Height
                   && Equals(left.Coordinates, right.Coordinates);

            public static bool operator ==(ImageCropperCrop left, ImageCropperCrop right)
                => Equals(left, right);

            public static bool operator !=(ImageCropperCrop left, ImageCropperCrop right)
                => !Equals(left, right);

            public override int GetHashCode()
            {
                unchecked
                {
                    // properties are, practically, readonly
                    // ReSharper disable NonReadonlyMemberInGetHashCode
                    var hashCode = Alias?.GetHashCode() ?? 0;
                    hashCode = (hashCode*397) ^ Width;
                    hashCode = (hashCode*397) ^ Height;
                    hashCode = (hashCode*397) ^ (Coordinates?.GetHashCode() ?? 0);
                    return hashCode;
                    // ReSharper restore NonReadonlyMemberInGetHashCode
                }
            }

            #endregion
        }

        [DataContract(Name = "imageCropCoordinates")]
        public class ImageCropperCropCoordinates : IEquatable<ImageCropperCropCoordinates>
        {
            [DataMember(Name = "x1")]
            public decimal X1 { get; set; }

            [DataMember(Name = "y1")]
            public decimal Y1 { get; set; }

            [DataMember(Name = "x2")]
            public decimal X2 { get; set; }

            [DataMember(Name = "y2")]
            public decimal Y2 { get; set; }

            #region IEquatable
            
            /// <inheritdoc />
            public bool Equals(ImageCropperCropCoordinates other)
                => other != null && (ReferenceEquals(this, other) || Equals(this, other));

            /// <inheritdoc />
            public override bool Equals(object obj)
                => obj != null && (ReferenceEquals(this, obj) || obj is ImageCropperCropCoordinates other && Equals(this, other));

            private static bool Equals(ImageCropperCropCoordinates left, ImageCropperCropCoordinates right)
                => left.X1 == right.X1
                   && left.X2 == right.X2
                   && left.Y1 == right.Y1
                   && left.Y2 == right.Y2;

            public static bool operator ==(ImageCropperCropCoordinates left, ImageCropperCropCoordinates right)
                => Equals(left, right);

            public static bool operator !=(ImageCropperCropCoordinates left, ImageCropperCropCoordinates right)
                => !Equals(left, right);

            public override int GetHashCode()
            {
                unchecked
                {
                    // properties are, practically, readonly
                    // ReSharper disable NonReadonlyMemberInGetHashCode
                    var hashCode = X1.GetHashCode();
                    hashCode = (hashCode*397) ^ Y1.GetHashCode();
                    hashCode = (hashCode*397) ^ X2.GetHashCode();
                    hashCode = (hashCode*397) ^ Y2.GetHashCode();
                    return hashCode;
                    // ReSharper restore NonReadonlyMemberInGetHashCode
                }
            }

            #endregion
        }
    }
}