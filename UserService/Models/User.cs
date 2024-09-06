using System;
using System.Collections.Generic;

namespace UserService.Models;

public partial class User
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string OtherData { get; set; } = null!;
}
