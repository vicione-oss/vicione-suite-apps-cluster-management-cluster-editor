using System.Collections.Generic;

namespace ViciOne.Ui.ClusterEditor.Models;

public interface IParentNode<T> where T : IParentNode<T>
{
    IEnumerable<T> Children { get; set; }
}
