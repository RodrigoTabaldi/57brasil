namespace Brasil57.Domain;
public sealed class VehicleMake
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public List<VehicleModel> Models { get; set; } = [];
}
public sealed class VehicleModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MakeId { get; set; }
    public VehicleMake Make { get; set; } = null!;
    public required string Name { get; set; }
    public List<VehicleVersion> Versions { get; set; } = [];
}
public sealed class VehicleVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ModelId { get; set; }
    public VehicleModel Model { get; set; } = null!;
    public required string Name { get; set; }
    public int StartYear { get; set; }
    public int EndYear { get; set; }
}
public sealed class ProductApplication
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid VehicleVersionId { get; set; }
    public VehicleVersion VehicleVersion { get; set; } = null!;
    public int StartYear { get; set; }
    public int EndYear { get; set; }
    public string? Restriction { get; set; }
}
