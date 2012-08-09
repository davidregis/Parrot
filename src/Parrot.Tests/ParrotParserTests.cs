﻿// -----------------------------------------------------------------------
// <copyright file="ParrotParserTests.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using Parrot.Mvc.Renderers;

namespace Parrot.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using Nodes;
    using Parrot;
    using NUnit.Framework;
    using Parser;
    using ValueType = Nodes.ValueType;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [TestFixture]
    public class ParrotParserTests
    {
        //so what are we tsting
        //block name == blcok name
        //block followed by block
        //block followed by ; with block
        //attribute name/values
        //parameter name/values
        public static Document Parse(string text)
        {
            Parser parser = new Parser();
            Document document;

            parser.Parse(new StringReader(text), out document);

            return document;
        }

        [TestCase("div")]
        [TestCase("a")]
        [TestCase("span")]
        public void ElementProducesBlockElement(string element)
        {
            var document = Parse(element);
            Assert.IsNotNull(document);
            Assert.AreEqual(element, document.Children[0].Name);
        }

        [TestCase("div1", "div2")]
        public void ElementFollowedByWhitespaceAndAnotherElementProduceTwoBlockElements(string element1, string element2)
        {
            var document = Parse(string.Format("{0} {1}", element1, element2));
            Assert.AreEqual(2, document.Children.Count);
        }


        public class IdTests
        {
            [TestCase("div", "sample-id")]
            public void ElementWithIdProducesBlockElementWithIdAttribute(string element, string id)
            {
                var document = Parse(string.Format("{0}#{1}", element, id));
                Assert.AreEqual(element, document.Children[0].Name);
                Assert.AreEqual("id", document.Children[0].Attributes[0].Key);
                Assert.AreEqual(id, document.Children[0].Attributes[0].Value);
            }

            [Test]
            public void ElementWithMultipleIdsThrowsParserException()
            {
                Assert.Throws<ParserException>(() => Parse("div#first-id#second-id"));
                Assert.Throws<ParserException>(() => Parse("div#first-id.class-name#second-id"));
            }
        }

        public class ClassTests
        {
            [TestCase("div", "sample-class")]
            public void ElementWithIdProducesBlockElementWithClassAttribute(string element, string @class)
            {
                var document = Parse(string.Format("{0}.{1}", element, @class));
                Assert.AreEqual("class", document.Children[0].Attributes[0].Key);
                Assert.AreEqual(@class, document.Children[0].Attributes[0].Value);
            }

            [TestCase("div", "class1", "class2", "class3")]
            public void ElementWithMultipleClassProducesBlockElementWithClassElementAndSpaceSeparatedClasses(string element, params string[] classes)
            {
                var document = Parse(string.Format("{0}.{1}", element, string.Join(".", classes)));
                Assert.AreEqual("class", document.Children[0].Attributes[0].Key);
                for (int i = 0; i < classes.Length; i++)
                {
                    Assert.AreEqual(classes[i], document.Children[0].Attributes[i].Value);
                }
            }
        }

        public class AttributeTests
        {
            [Test]
            public void ElementWithSingleAttributeProducesBlockElementWithAttributes()
            {
                var document = Parse("div[attr1='value1']");
                Assert.AreEqual(1, document.Children[0].Attributes.Count);
            }

            [Test]
            public void ElementWithMultipleAttributesProducesBlockElementWithMultipleAttributes()
            {
                var document = Parse("div[attr1='value1' attr2='value2']");
                Assert.AreEqual(2, document.Children[0].Attributes.Count);
            }

            [Test]
            public void ElementWithAttributeValueNotSurroundedByQuotesProducesAttributeWithValueTypeAsProperty()
            {
                var document = Parse("div[attr1=Value]");
                Assert.AreEqual(1, document.Children[0].Attributes.Count);
                Assert.AreEqual(ValueType.Property, document.Children[0].Attributes[0].ValueType);
            }

            [Test]
            public void ElementWithAttributeValueSetTothisProducesAttributeWithValueTypeAsLocal()
            {
                var document = Parse("div[attr1=this]");
                Assert.AreEqual(1, document.Children[0].Attributes.Count);
                Assert.AreEqual(ValueType.Local, document.Children[0].Attributes[0].ValueType);
            }

            [Test]
            public void ElementWithAttributeWithNoValueProducesAttributeWithValueSetToNull()
            {
                var document = Parse("div[attr]");
                Assert.IsNull(document.Children[0].Attributes[0].Value);
                Assert.AreEqual("attr", document.Children[0].Attributes[0].Key);
            }

            [Test]
            public void ElementWithOutElementDeclarationButWithClassDeclarationCreatesDivElement()
            {
                var document = Parse(".sample-class");
                Assert.IsNullOrEmpty(null, document.Children[0].Name);
                Assert.AreEqual("class", document.Children[0].Attributes[0].Key);
                Assert.AreEqual("sample-class", document.Children[0].Attributes[0].Value);
            }

            [Test]
            public void ElementWithInvalidAttributeDeclarationsThrowsParserException()
            {
                Assert.Throws<ParserException>(() => Parse("div[attr1=]"));
                Assert.Throws<ParserException>(() => Parse("div[]"));
                Assert.Throws<ParserException>(() => Parse("div[=\"value only\"]"));
                Assert.Throws<ParserException>(() => Parse("div[attr1=\"missing closing quote]"));
                Assert.Throws<ParserException>(() => Parse("div[attr1='missing closing quote]"));
            }

            [Test]
            public void StatementWithGTChildCreatesBlockWithOneChild()
            {
                var document = Parse("div > span");
                Assert.AreEqual("div", document.Children[0].Name);
                Assert.AreEqual("span", document.Children[0].Children[0].Name);

                document = Parse("div > span > span");
                Assert.AreEqual("div", document.Children[0].Name);
                Assert.AreEqual("span", document.Children[0].Children[0].Name);
                Assert.AreEqual("span", document.Children[0].Children[0].Children[0].Name);

            }

            [Test]
            public void StringLiteralParserTests()
            {
                var parts = new StringLiteral("\"this :is awesome :right\"").Value;

                Assert.AreEqual(4, parts.Count);
                Assert.AreEqual(StringLiteralPartType.Literal, parts[0].Type);
                Assert.AreEqual(StringLiteralPartType.Encoded, parts[1].Type);
                Assert.AreEqual(StringLiteralPartType.Literal, parts[2].Type);
                Assert.AreEqual(StringLiteralPartType.Encoded, parts[1].Type);

                Assert.AreEqual("this ", parts[0].Data);
                Assert.AreEqual("is", parts[1].Data);
                Assert.AreEqual(" awesome ", parts[2].Data);
                Assert.AreEqual("right", parts[3].Data);

                parts = new StringLiteral("\"this contains a : but not a keyword\"").Value;
                Assert.AreEqual(1, parts.Count);
                Assert.AreEqual(StringLiteralPartType.Literal, parts[0].Type);

                parts = new StringLiteral("\":keyword_only\"").Value;
                Assert.AreEqual(1, parts.Count);
                Assert.AreEqual(StringLiteralPartType.Encoded, parts[0].Type);
                Assert.AreEqual("keyword_only", parts[0].Data);

                parts = new StringLiteral("\":keyword_first followed by more words\"").Value;
                Assert.AreEqual(2, parts.Count);
                Assert.AreEqual(StringLiteralPartType.Encoded, parts[0].Type);
                Assert.AreEqual(StringLiteralPartType.Literal, parts[1].Type);

                parts = new StringLiteral("\":keyword.with.dot\"").Value;
                Assert.AreEqual(1, parts.Count);
                Assert.AreEqual(StringLiteralPartType.Encoded, parts[0].Type);
                Assert.AreEqual("keyword.with.dot", parts[0].Data);

                parts = new StringLiteral("\"this is an :: escaped colon\"").Value;
                Assert.AreEqual(1, parts.Count);
                Assert.AreEqual("this is an : escaped colon", parts[0].Data);

                parts = new StringLiteral("\"this =is awesome =right\"").Value;

                Assert.AreEqual(4, parts.Count);
                Assert.AreEqual(StringLiteralPartType.Literal, parts[0].Type);
                Assert.AreEqual(StringLiteralPartType.Raw, parts[1].Type);
                Assert.AreEqual(StringLiteralPartType.Literal, parts[2].Type);
                Assert.AreEqual(StringLiteralPartType.Raw, parts[1].Type);

                Assert.AreEqual("this ", parts[0].Data);
                Assert.AreEqual("is", parts[1].Data);
                Assert.AreEqual(" awesome ", parts[2].Data);
                Assert.AreEqual("right", parts[3].Data);

                parts = new StringLiteral("\"this contains a = but not a keyword\"").Value;
                Assert.AreEqual(1, parts.Count);
                Assert.AreEqual(StringLiteralPartType.Literal, parts[0].Type);

                parts = new StringLiteral("\"=keyword_only\"").Value;
                Assert.AreEqual(1, parts.Count);
                Assert.AreEqual(StringLiteralPartType.Raw, parts[0].Type);
                Assert.AreEqual("keyword_only", parts[0].Data);

                parts = new StringLiteral("\"=keyword_first followed by more words\"").Value;
                Assert.AreEqual(2, parts.Count);
                Assert.AreEqual(StringLiteralPartType.Raw, parts[0].Type);
                Assert.AreEqual(StringLiteralPartType.Literal, parts[1].Type);

                parts = new StringLiteral("\"=keyword.with.dot\"").Value;
                Assert.AreEqual(1, parts.Count);
                Assert.AreEqual(StringLiteralPartType.Raw, parts[0].Type);
                Assert.AreEqual("keyword.with.dot", parts[0].Data);

                parts = new StringLiteral("\"this is an == escaped equals\"").Value;
                Assert.AreEqual(1, parts.Count);
                Assert.AreEqual("this is an = escaped equals", parts[0].Data);

            }
        }
    }

}
