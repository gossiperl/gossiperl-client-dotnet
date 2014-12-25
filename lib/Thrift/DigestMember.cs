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
  public partial class DigestMember : TBase
  {

    public string Member_name { get; set; }

    public string Member_ip { get; set; }

    public int Member_port { get; set; }

    public long Member_heartbeat { get; set; }

    public DigestMember() {
    }

    public DigestMember(string member_name, string member_ip, int member_port, long member_heartbeat) : this() {
      this.Member_name = member_name;
      this.Member_ip = member_ip;
      this.Member_port = member_port;
      this.Member_heartbeat = member_heartbeat;
    }

    public void Read (TProtocol iprot)
    {
      bool isset_member_name = false;
      bool isset_member_ip = false;
      bool isset_member_port = false;
      bool isset_member_heartbeat = false;
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
              Member_name = iprot.ReadString();
              isset_member_name = true;
            } else { 
              TProtocolUtil.Skip(iprot, field.Type);
            }
            break;
          case 2:
            if (field.Type == TType.String) {
              Member_ip = iprot.ReadString();
              isset_member_ip = true;
            } else { 
              TProtocolUtil.Skip(iprot, field.Type);
            }
            break;
          case 3:
            if (field.Type == TType.I32) {
              Member_port = iprot.ReadI32();
              isset_member_port = true;
            } else { 
              TProtocolUtil.Skip(iprot, field.Type);
            }
            break;
          case 4:
            if (field.Type == TType.I64) {
              Member_heartbeat = iprot.ReadI64();
              isset_member_heartbeat = true;
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
      if (!isset_member_name)
        throw new TProtocolException(TProtocolException.INVALID_DATA);
      if (!isset_member_ip)
        throw new TProtocolException(TProtocolException.INVALID_DATA);
      if (!isset_member_port)
        throw new TProtocolException(TProtocolException.INVALID_DATA);
      if (!isset_member_heartbeat)
        throw new TProtocolException(TProtocolException.INVALID_DATA);
    }

    public void Write(TProtocol oprot) {
      TStruct struc = new TStruct("DigestMember");
      oprot.WriteStructBegin(struc);
      TField field = new TField();
      field.Name = "member_name";
      field.Type = TType.String;
      field.ID = 1;
      oprot.WriteFieldBegin(field);
      oprot.WriteString(Member_name);
      oprot.WriteFieldEnd();
      field.Name = "member_ip";
      field.Type = TType.String;
      field.ID = 2;
      oprot.WriteFieldBegin(field);
      oprot.WriteString(Member_ip);
      oprot.WriteFieldEnd();
      field.Name = "member_port";
      field.Type = TType.I32;
      field.ID = 3;
      oprot.WriteFieldBegin(field);
      oprot.WriteI32(Member_port);
      oprot.WriteFieldEnd();
      field.Name = "member_heartbeat";
      field.Type = TType.I64;
      field.ID = 4;
      oprot.WriteFieldBegin(field);
      oprot.WriteI64(Member_heartbeat);
      oprot.WriteFieldEnd();
      oprot.WriteFieldStop();
      oprot.WriteStructEnd();
    }

    public override string ToString() {
      StringBuilder sb = new StringBuilder("DigestMember(");
      sb.Append("Member_name: ");
      sb.Append(Member_name);
      sb.Append(",Member_ip: ");
      sb.Append(Member_ip);
      sb.Append(",Member_port: ");
      sb.Append(Member_port);
      sb.Append(",Member_heartbeat: ");
      sb.Append(Member_heartbeat);
      sb.Append(")");
      return sb.ToString();
    }

  }

}