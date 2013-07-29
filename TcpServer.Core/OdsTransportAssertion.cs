using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Web.Services3;
using Microsoft.Web.Services3.Design;


namespace TestOds {
    public class OdsTransportAssertion : UsernameOverTransportAssertion {
        public override SoapFilter CreateClientOutputFilter(FilterCreationContext context) {
            return new OdsClientOutputFilter(this);
        }

        private class OdsClientOutputFilter : ClientOutputFilter {
            private static readonly XmlNamespaceManager Nsmanager;

            static OdsClientOutputFilter() {
                Nsmanager = new XmlNamespaceManager(new NameTable());
                Nsmanager.AddNamespace("wsse", @"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd");
                Nsmanager.AddNamespace("soap", @"http://schemas.xmlsoap.org/soap/envelope/");
                Nsmanager.AddNamespace("wsu", @"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");
            }
            //----------------------------------------------------------------------[]
            public OdsClientOutputFilter(UsernameOverTransportAssertion assertion) : base(assertion) {
            }

            public override SoapFilterResult ProcessMessage(SoapEnvelope envelope) {
                base.ProcessMessage(envelope);

                ModifyUsernameTokenElement(envelope);

                return SoapFilterResult.Continue;
            }

            private static void ModifyUsernameTokenElement(SoapEnvelope envelope) {
                XmlNode usernameTokenNode =
                    envelope.SelectSingleNode("/soap:Envelope/soap:Header/wsse:Security/wsse:UsernameToken", Nsmanager);
                usernameTokenNode.Attributes.RemoveAll();

                RemoveAllChildExcept(usernameTokenNode, UsernameAndTokenSelector);
            }

            //----------------------------------------------------------------------[]
            private static void RemoveAllChildExcept(XmlNode target, Predicate<XmlNode> removeCondition) {
                List<XmlNode> toRemove = new List<XmlNode>();
                foreach (XmlElement childNode in target.ChildNodes) {
                    if (removeCondition(childNode))
                        toRemove.Add(childNode);
                }
                foreach (XmlNode xmlNode in toRemove) {
                    target.RemoveChild(xmlNode);
                }
            }

            //----------------------------------------------------------------------[]
            private static bool UsernameAndTokenSelector(XmlNode node) {
                return !(node.Name == "wsse:Username" || node.Name == "wsse:Password");
            }
            
        }
    }
}