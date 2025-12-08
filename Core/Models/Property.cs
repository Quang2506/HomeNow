using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    [Table("properties", Schema = "public")]
    public class Property
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("title")]
        public string Title { get; set; }                // Tiêu đề gốc (thường tiếng Trung)

        [Column("property_type")]
        public string PropertyType { get; set; }        // Loại nhà: apartment / room...

        [Column("price_per_month")]
        public decimal? PricePerMonth { get; set; }     // Giá thuê theo tháng

        [Column("area_m2")]
        public decimal? AreaM2 { get; set; }            // Diện tích m2

        [Column("address")]
        public string Address { get; set; }             // Địa chỉ đầy đủ

        [Column("district")]
        public string District { get; set; }            // Quận

        [Column("city")]
        public string City { get; set; }                // Thành phố

        [Column("room_type")]
        public string RoomType { get; set; }            // Bố cục: 1 phòng ngủ...

        [Column("is_vr_available")]
        public bool IsVrAvailable { get; set; }         // Nhà có VR không?

        [Column("status")]
        public string Status { get; set; }              // Trạng thái (available / rented)

        [Column("cover_image_url")]
        public string CoverImageUrl { get; set; }       // Ảnh cover

        [Column("area_name")]
        public string AreaName { get; set; }            // Khu vực nhỏ

        [Column("community_name")]
        public string CommunityName { get; set; }       // Tên khu dân cư

        [Column("orientation")]
        public string Orientation { get; set; }         // Hướng nhà

        [Column("price_unit")]
        public string PriceUnit { get; set; }           // 元/月

        [Column("display_title")]
        public string DisplayTitle { get; set; }        // Tiêu đề hiển thị

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        // Các bản dịch liên quan (VI/EN/ZH)
        public virtual ICollection<PropertyTranslation> Translations { get; set; }

        // Danh sách các phòng VR thuộc nhà này
        public virtual ICollection<VrScene> Scenes { get; set; }
    }
}
