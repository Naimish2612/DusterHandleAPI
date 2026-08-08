namespace DUSTER.EComm.Services.Modules.Notifications.Models
{
    [Table("tbl_notification")]
    public class NotificationModel : BaseEntity
    {
        public NotificationModel()
        {

        }

        [Key]
        public long notification_id { get; set; }
        public long user_code { get; set; }
        public string notification_type { get; set; }
        public string notification_key { get; set; }
        public string notification_value { get; set; }
        public DateTime notification_date { get; set; }

        /// <summary>
        /// 0 = PENDING, 1= SUCCESS, 2=ERROR
        /// </summary>
        public short status { get; set; }
        public string notification_body { get; set; }
    }

    public class NotificationValidator : AbstractValidator<NotificationModel>
    {
        public NotificationValidator() { }
    }
}
