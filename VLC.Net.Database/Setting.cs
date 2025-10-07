using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VLC.Net.Database;

public class Setting
{
    [Key] 
    [StringLength(100)]
    public string Key { get; set; } = null!;
    
    [StringLength(1024)]
    public string? Value { get; set; } = null!;
}