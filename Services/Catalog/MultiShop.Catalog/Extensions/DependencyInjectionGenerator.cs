using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using MultiShop.Catalog.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MultiShop.Catalog.Extensions
{
    [Generator] //source generator olduğunu işaretler.
    public class DependencyInjectionGenerator : ISourceGenerator
    {public void Initialize(GeneratorInitializationContext context)
        {
            var syntaxReceiver = new SyntaxReceiver(); // SyntaxReceiver nesnesini oluştur
            context.RegisterForSyntaxNotifications(() => syntaxReceiver);
            //context.RegisterForSyntaxNotifications(() => new SyntaxReceiver()); //generator'ın syntax bağımlılıklarını çözmek için bir alıcı başlatır.

        }
        public void Execute(GeneratorExecutionContext context)
        {

            if (context.SyntaxReceiver is not SyntaxReceiver receiver) //Initialize metodu ile receiver'dan gelen dataları kontrol ederiz.
                return;

            var sb = new StringBuilder();
            sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            sb.AppendLine("public static class GeneratedServiceRegistration");
            sb.AppendLine("{");
            sb.AppendLine("    public static void AddGeneratedServices(this IServiceCollection services)");
            sb.AppendLine("    {");

            var attributeSymbol = context.Compilation.GetTypeByMetadataName("ServiceAttribute"); //Attribute sınıfının ismini yazarız

            foreach (var classDeclaration in receiver.CandidateClasses) //receiver ile aldığımız sınıfların hepsini dolaşırız.
            {
                var model = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree); //semantik modelini alırız çünkü gelen sınıfın ve bu sınıfın sahip olduğu attribute'ünün sembolünü ve türünü analiz etmemiz gerekir. yani burada sadece syntax'e dayanmadan bir işlem olması gerekli. ayrıca hata ayıklama konusunda da bize yardımcı olacaktır.
                var classSymbol = model.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol; //sınıf sembolünü alırız. INamedTypeSymbol ile beraber ise sınıfın türünü ve implemente ettiği interface bilgisini alıyoruz.

                var attributeData = classSymbol?.GetAttributes().FirstOrDefault(ad =>
                    ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));/*
                                                                                                    * classSymbol.GetAttributes -> sınıfın aldığı tüm attribute'ları alır.
                                                                                                    * ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default) --> bu kodlarla beraber ise gelen attrübute'un bizim verdiğimiz attribute olan yani örnekteki "ServiceAttribute" olup olmadığını arayıp kontrol eder.
                                                                                                    */

                if (attributeData != null)
                {
                    var lifetime = (ServiceLifetime)attributeData.ConstructorArguments[0].Value; //burda attribüte için verdiğimiz property değerlerini alır. yani örnekteki ServiceLifetime değeri.
                    var interfaces = classSymbol.Interfaces; //sınıfın implemente ettiği interface'i alırız.

                    foreach (var @interface in interfaces)
                    {
                        sb.AppendLine($"        services.Add(new ServiceDescriptor(typeof({@interface}), typeof({classSymbol}), ServiceLifetime.{lifetime}));"); //interface ve sınıfı DI container'a ekleriz.
                    }
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            context.AddSource("GeneratedServiceRegistration.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8)); //generator tarafından oluşturulan kodu kaynak dosya olarak ekleriz. bu en önemli kısımlardan biridir çünkü derleme esnasında kodu bu çıktıya eklememiz gerekiyor ki kodun bir parçası olsun.
            /*açıklama
             *context.AddSource:
                context.AddSource metodu, generator tarafından oluşturulan kodu derleme sürecine eklemek için kullanılır. Bu metod, iki argüman almaktadır; kaynak dosyanın adını ve dosyanın içeriğini.
            ----------------------------------------------------------

            Kaynak Dosya Adı ("GeneratedServiceRegistration.g.cs"):
                "GeneratedServiceRegistration.g.cs": Bu, oluşturulan kaynak dosyanın adıdır. Dosya adının .g.cs ile bitmesi, bunun generator
            tarafından oluşturulan bir kaynak dosya olduğunu belirtir.
            Dosyanın adı generator tarafından oluşturulan kodun nereden geldiğini belirlemek için önemlidir ve kodu daha sonra tanımlamak ve hata ayıklamak için bize yardımcı olabilir.
           ----------------------------------------------------------
            Kaynak Dosyanın İçeriği (SourceText.From):
                SourceText.From(sb.ToString(), Encoding.UTF8): Bu, oluşturulan string içeriği utf-8 kodlamasıyla SourceText formatına 
            dönüştürür.
            sb.ToString(): StringBuilder nesnesinde biriken tüm kodu string olarak alır.
            Encoding.UTF8: Kaynak dosyanın UTF-8 kodlamasıyla oluşturulacağını belirtir.
             */


        }

        private class SyntaxReceiver : ISyntaxReceiver
        {
            public List<ClassDeclarationSyntax> CandidateClasses { get; } = new List<ClassDeclarationSyntax>(); //işaretlenmiş sınıfları tutan bir liste.

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // ClassDeclarationSyntax tipinde olan ve attribute listesi olan sınıfları alır.
                if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax &&
                    classDeclarationSyntax.AttributeLists.Count > 0)
                {
                    // Sınıfın adını ve attribute listesini yazdırır.
                    Console.WriteLine($"Found class with attributes: {classDeclarationSyntax.Identifier}");
                    foreach (var attributeList in classDeclarationSyntax.AttributeLists)
                    {
                        Console.WriteLine($"    Attributes: {attributeList}");
                    }

                    // Sınıfı aday sınıflar listesine ekler.
                    CandidateClasses.Add(classDeclarationSyntax); //işaretlenmiş olan sınıfı listeye ekleriz
                }
            }

        }

    }

}
