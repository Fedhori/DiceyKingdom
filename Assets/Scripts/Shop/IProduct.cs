public interface IProduct
{
    ProductType ProductType { get; }
    string Id { get; }
    int Price { get; }
    UnityEngine.Sprite Icon { get; }
    bool Sold { get; set; }
}
