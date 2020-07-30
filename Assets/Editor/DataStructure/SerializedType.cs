using System;
using System.Collections.Generic;

public class SerializedType
{
    public Int32 ClassID;
    public bool IsStrippedType;
    public Int16 ScriptTypeIndex = -1;
    public List<TypeTreeNode> Nodes;
    public byte[] ScriptID; //Hash128
    public byte[] OldTypeHash; //Hash128
    public Int32[] TypeDependencies;
   
}