# 🦺 EPIFlow — Sistema de Controle de EPIs

O **EPIFlow** é um sistema SaaS desenvolvido em **ASP.NET MVC (.NET 9)** que oferece um controle completo de **Equipamentos de Proteção Individual (EPIs)**, desde o **cadastro**, **controle de estoque**, **entregas aos colaboradores** e **rastreabilidade por biometria digital**.  
O objetivo é garantir **segurança, conformidade legal (NR-06)** e **eficiência operacional** no gerenciamento de EPIs dentro das empresas.

---

## 🚀 Funcionalidades Principais

### 🔐 Autenticação e Segurança
- Login e gerenciamento de usuários com **ASP.NET Identity**.  
- Controle de acesso por **perfis (Roles)**: *Administrador*, *Segurança do Trabalho* e *Colaborador*.  
- Armazenamento seguro do token de sessão.  

### 🏭 Gestão de EPIs e Estoques
- Cadastro completo de **EPIs**, **tipos de EPI**, **categorias** e **fabricantes**.  
- Controle de **entradas e saídas de estoque** com histórico detalhado.  
- **Conciliação de saldos** entre depósitos e relatórios de movimentação.  

### 👷 Entregas com Biometria
- Registro de entregas com **leitura biométrica (DigitalPersona 4500)**.  
- Associação automática do colaborador à entrega.  
- Emissão de **comprovante digital de entrega** via QuestPDF.  

### 🧾 Relatórios e Dashboards
- Relatórios de entrega, saldo e validade de EPIs (via **QuestPDF**).  
- Dashboard inicial com **indicadores de consumo, estoque e colaboradores atendidos**.  
- Exportação em PDF e Excel.  

### ☁️ Multiempresa (SaaS)
- Estrutura multi-tenant: cada empresa possui seu próprio ambiente isolado.  
- Usuários *master* podem gerenciar empresas (tenants) e ver estatísticas globais.  

---

## 🧱 Tecnologias Utilizadas

| Camada | Tecnologias |
|--------|--------------|
| Backend | ASP.NET MVC (.NET 9), C#, Entity Framework Core, SQL Server |
| Frontend | Bootstrap 5, JavaScript, SweetAlert2, DataTables |
| Relatórios | QuestPDF |
| Autenticação | ASP.NET Identity |
| Biometria | HID DigitalPersona 4500 SDK |
| Banco de Dados | Microsoft SQL Server |

---

## ⚙️ Configuração do Ambiente

### 🔧 Requisitos
- .NET 9 SDK  
- SQL Server (local ou remoto)  
- Visual Studio 2022 / VS Code  
- HID DigitalPersona SDK (para módulo de biometria)

### 📁 Clonar o Projeto
```bash
git clone https://github.com/seuusuario/EPIFlow.git
cd EPIFlow
⚙️ Configurar o appsettings.json
Edite a string de conexão:

json
Copiar código
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=EPIFlow;User Id=sa;Password=Admin@123;TrustServerCertificate=True;"
}
▶️ Executar a Aplicação
bash
Copiar código
dotnet ef database update
dotnet run
Acesse no navegador:
👉 http://localhost:5000

🧩 Estrutura do Projeto
vbnet
Copiar código
EPIFlow/
│
├── Controllers/
│   ├── ColaboradoresController.cs
│   ├── EPIsController.cs
│   ├── EntregasController.cs
│   ├── EstoquesController.cs
│   └── AdminController.cs
│
├── Models/
│   ├── Colaborador.cs
│   ├── EPI.cs
│   ├── EntregaEPI.cs
│   ├── Almoxarifado.cs
│   └── TipoEPI.cs
│
├── Views/
│   ├── Shared/
│   ├── Entregas/
│   ├── EPIs/
│   └── Estoques/
│
├── Services/
│   ├── BiometriaService.cs
│   ├── RelatorioService.cs
│   └── EstoqueService.cs
│
├── wwwroot/
│   ├── css/
│   ├── js/
│   └── imgs/
│
└── EPIFlow.sln
📊 Relatórios Disponíveis
Entregas de EPIs por colaborador

Consumo por período

Controle de validade e reposição

Movimentação de estoque por almoxarifado

Saldos consolidados

Todos os relatórios são exportáveis em PDF (QuestPDF) e Excel.

🧠 Próximas Funcionalidades
📱 Aplicativo mobile para registro de entrega via .NET MAUI

🧾 Assinatura eletrônica do termo de entrega

🧮 Integração com ERP Senior Sapiens

🔔 Alertas automáticos de EPIs vencendo

👨‍💻 Autor
Carlos Eduardo Pereira Dutra
Desenvolvedor Full-Stack | .NET | SQL | SaaS
📧 carloseduardopereiradutra@outlook.com
🔗 LinkedIn

📝 Licença
Este projeto está licenciado sob a MIT License.
Consulte o arquivo LICENSE para mais informações.

© 2025 — EPIFlow. Todos os direitos reservados.