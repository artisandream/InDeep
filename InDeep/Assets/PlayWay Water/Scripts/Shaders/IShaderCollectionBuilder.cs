
namespace PlayWay.Water
{
	/// <summary>
	/// A class that adds stuff to shader collection during its building process.
	/// </summary>
	public interface IShaderCollectionBuilder
	{
		void Write(ShaderCollection collection);
	}
}
