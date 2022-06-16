using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RDFEngine;
namespace FactographyView.Models
{
    /// Вспомогательный класс - группировка списков обратных свойств
    public class InversePropType
    {
        public string Prop;
        public InverseType[] lists; 
    }
    /// Вспомогательный класс - группировка списков по типам ссылающихся записей
    public class InverseType
    {
        public string Tp;
        public RProperty[] list;
    }

    /// <summary>
    /// Класс состоит из элементов и структур, требуемых для отрисовки портрета
    /// Логическая структура отрисовки: тип, идентификатор, name, uri
    /// Потом - массив полей и прямых свойств
    /// Потом - массив групп обратных свойств. Каждая группа определяется именем предиката ОС и типом записи ОС
    /// </summary>
    public class P3Model
    {
        public string Id, Tp;
        public RProperty[] row;
        public InversePropType[] inv;
        public P3Model(RRecord erec)
        {
            this.Id = erec.Id;
            this.Tp = erec.Tp;

            List<RInverse> inverselist;
            this.row = ConvertRecordToRowarr(erec, null, out inverselist);
            
            List<InversePropType> lip = new List<InversePropType>();
            foreach (var pair in inverselist.GroupBy<RInverse, string>(p => p.Prop))
            {
                var k = pair.Key;
                var v = pair.ToArray();//.GroupBy<RInverse, string>(ri => ri.IRec.Tp).ToArray();
                var w = v.GroupBy<RInverse, string>(ris => ris.IRec.Tp)
                    .Select(pa => new InverseType()
                    {
                        Tp = pa.Key,
                        list = pa.Select<RInverse, RInverse>(q =>
                        {
                            var irec = ConvertRecordToRowarr(q.IRec, k, out List<RInverse> ilist);
                            //return new RInverse { Prop = q.Prop, IRec = q.IRec };
                            return new RInverse { Prop = q.Prop, 
                                IRec = new RRecord { Id=q.IRec.Id, Tp = q.IRec.Tp, Props = irec } };
                        }).ToArray()
                    }).ToArray();
                lip.Add(new InversePropType() { Prop = k, lists = w });
            }
            this.inv = lip.ToArray();

        }

        private static RProperty[] ConvertRecordToRowarr(RRecord erec, string forbidden, out List<RInverse> inverselist)
        {
            // определяем номер онтологического определения класса (типа) Tp
            int cldefnom = Infobase.ront.dicOnto[erec.Tp];
            // найдем определение записи и словарь для этой записи
            var recdef = Infobase.ront.rontology[cldefnom];
            var de = Infobase.ront.dicsProps[cldefnom];
            RProperty[] rowarr;
            // создадим массив свойств по размеру recdef и разметим его пустышками
            rowarr = recdef.Props.Select<RProperty, RProperty>(p =>
            {
                //if (p is RField) return new RField { Prop = p.Prop };
                if (p is RLink && ((RLink)p).Resource != forbidden) return new RLink { Prop = ((RLink)p).Resource };
                return null;
            }).Where(p1 => p1 != null).ToArray();

            //List<RProperty> rowlist = new List<RProperty>();
            inverselist = new List<RInverse>();
            foreach (var p in erec.Props)
            {
                if (p is RInverse)
                {
                    RInverse ri = (RInverse)p;
                    inverselist.Add(new RInverse() { Prop = ri.Prop, IRec = ri.IRec });
                }
                else
                {
                    int forbidden_ind = forbidden==null? rowarr.Length : de[forbidden];
                    int ind = de[p.Prop];
                    if (ind > forbidden_ind) ind--;
                    if (p is RField)
                    {
                        RField f = (RField)p;
                        //if (f.Prop == "name") name = f.Value;
                        //else if (f.Prop == "uri") uri = f.Value;
                        //else
                        {
                            rowarr[ind] = new RField() { Prop = f.Prop, Value = f.Value };
                            //rowlist.Add(new RField() { Prop = f.Prop, Value = f.Value });
                        }
                    }
                    else if (p is RDirect)
                    {
                        RDirect d = (RDirect)p;
                        rowarr[ind] = new RDirect() { Prop = d.Prop, DRec = d.DRec };
                        //rowlist.Add(new RDirect() { Prop = d.Prop, DRec = d.DRec });
                    }
                }
            }
            return rowarr;
        }
    }

}
