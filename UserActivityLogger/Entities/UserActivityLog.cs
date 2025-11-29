
namespace UserActivityLogger.Entities;

public class UserActivityLog
{
    [Key]
    public int Id { get; set; }


    public int ResponseStatusCode { get; set; }

    [Required]
    [MaxLength(500)]
    public string Event { get; set; } = null!;

    
    [MaxLength(50)]
    public string? IPAddress { get; set; }

   
    [MaxLength(2000)]
    public string? Path { get; set; }

   
    [MaxLength(10)]
    public string? Method { get; set; }

    public string? AdditionalData { get; set; }

    public DateTime DateEvent { get; set; } = DateTime.Now;


    [MaxLength(450)]
    public string? UserId { get; set; }

    /// 
    /// Navigation property to the user entity.
    /// Note: This should be configured in your DbContext to point to your actual user entity.
    /// 
    [ForeignKey(nameof(UserId))]
    public virtual object? User { get; set; }
}