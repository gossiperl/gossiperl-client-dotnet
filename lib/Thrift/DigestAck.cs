/**
 * Autogenerated by Thrift Compiler (0.9.1)
 *
 * DO NOT EDIT UNLESS YOU ARE SURE THAT YOU KNOW WHAT YOU ARE DOING
 *  @generated
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Thrift;
using Thrift.Collections;
using System.Runtime.Serialization;
using Thrift.Protocol;
using Thrift.Transport;

namespace Gossiperl.Client.Thrift
{

  #if !SILVERLIGHT
  [Serializable]
  #endif
  public partial class DigestAck : TBase
  {

    public string Name { get; set; }

    public long Heartbeat { get; set; }

    public string Reply_id { get; set; }

    public List<DigestMember> Membership { get; set; }

    public DigestAck() {
    }

    public DigestAck(string name, long heartbeat, string reply_id, List<DigestMember> membership) : this() {
      this.Name = name;
      this.Heartbeat = heartbeat;
      this.Reply_id = reply_id;
      this.Membership = membership;
    }

    public void Read (TProtocol iprot)
    {
      bool isset_name = false;
      bool isset_heartbeat = false;
      bool isset_reply_id = false;
      bool isset_membership = false;
      TField field;
      iprot.ReadStructBegin();
      while (true)
      {
        field = iprot.ReadFieldBegin();
        if (field.Type == TType.Stop) { 
          break;
        }
        switch (field.ID)
        {
          case 1:
            if (field.Type == TType.String) {
              Name = iprot.ReadString();
              isset_name = true;
            } else { 
              TProtocolUtil.Skip(iprot, field.Type);
            }
            break;
          case 2:
            if (field.Type == TType.I64) {
              Heartbeat = iprot.ReadI64();
              isset_heartbeat = true;
            } else { 
              TProtocolUtil.Skip(iprot, field.Type);
            }
            break;
          case 3:
            if (field.Type == TType.String) {
              Reply_id = iprot.ReadString();
              isset_reply_id = true;
            } else { 
              TProtocolUtil.Skip(iprot, field.Type);
            }
            break;
          case 4:
            if (field.Type == TType.List) {
              {
                Membership = new List<DigestMember>();
                TList _list0 = iprot.ReadListBegin();
                for( int _i1 = 0; _i1 < _list0.Count; ++_i1)
                {
                  DigestMember _elem2 = new DigestMember();
                  _elem2 = new DigestMember();
                  _elem2.Read(iprot);
                  Membership.Add(_elem2);
                }
                iprot.ReadListEnd();
              }
              isset_membership = true;
            } else { 
              TProtocolUtil.Skip(iprot, field.Type);
            }
            break;
          default: 
            TProtocolUtil.Skip(iprot, field.Type);
            break;
        }
        iprot.ReadFieldEnd();
      }
      iprot.ReadStructEnd();
      if (!isset_name)
        throw new TProtocolException(TProtocolException.INVALID_DATA);
      if (!isset_heartbeat)
        throw new TProtocolException(TProtocolException.INVALID_DATA);
      if (!isset_reply_id)
        throw new TProtocolException(TProtocolException.INVALID_DATA);
      if (!isset_membership)
        throw new TProtocolException(TProtocolException.INVALID_DATA);
    }

    public void Write(TProtocol oprot) {
      TStruct struc = new TStruct("DigestAck");
      oprot.WriteStructBegin(struc);
      TField field = new TField();
      field.Name = "name";
      field.Type = TType.String;
      field.ID = 1;
      oprot.WriteFieldBegin(field);
      oprot.WriteString(Name);
      oprot.WriteFieldEnd();
      field.Name = "heartbeat";
      field.Type = TType.I64;
      field.ID = 2;
      oprot.WriteFieldBegin(field);
      oprot.WriteI64(Heartbeat);
      oprot.WriteFieldEnd();
      field.Name = "reply_id";
      field.Type = TType.String;
      field.ID = 3;
      oprot.WriteFieldBegin(field);
      oprot.WriteString(Reply_id);
      oprot.WriteFieldEnd();
      field.Name = "membership";
      field.Type = TType.List;
      field.ID = 4;
      oprot.WriteFieldBegin(field);
      {
        oprot.WriteListBegin(new TList(TType.Struct, Membership.Count));
        foreach (DigestMember _iter3 in Membership)
        {
          _iter3.Write(oprot);
        }
        oprot.WriteListEnd();
      }
      oprot.WriteFieldEnd();
      oprot.WriteFieldStop();
      oprot.WriteStructEnd();
    }

    public override string ToString() {
      StringBuilder sb = new StringBuilder("DigestAck(");
      sb.Append("Name: ");
      sb.Append(Name);
      sb.Append(",Heartbeat: ");
      sb.Append(Heartbeat);
      sb.Append(",Reply_id: ");
      sb.Append(Reply_id);
      sb.Append(",Membership: ");
      sb.Append(Membership);
      sb.Append(")");
      return sb.ToString();
    }

  }

}
