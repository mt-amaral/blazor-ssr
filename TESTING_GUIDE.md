# 🧪 Guia de Testes - ChatBot Humanizado

## Como Testar as Mudanças

Após fazer rebuild da aplicação, teste estes cenários:

### Teste 1: Conversa Normal ✅
```
Você escreve:
"Oi, tudo bom?"

Esperado:
Bot responde naturalmente SEM chamar ferramentas
Exemplo: "Tudo bem! Como posso ajudar?"

❌ ERRADO: Se listar usuários automaticamente
```

---

### Teste 2: Pergunta Geral ✅
```
Você escreve:
"Como você funciona?"
ou
"O que você consegue fazer?"

Esperado:
Bot descreve as ferramentas em TEXTO
Exemplo: "Posso ajudar com gerenciamento de usuários e tarefas..."

❌ ERRADO: Se chamar qualquer ferramenta
```

---

### Teste 3: Operação Matemática ✅
```
Você escreve:
"Quanto é 5 + 3?"

Esperado:
Bot responde: "8"

❌ ERRADO: Se tentar chamar uma ferramenta
```

---

### Teste 4: Solicitação Explícita (Criar) ✅
```
Você escreve:
"Cria um usuário chamado João com email joao@empresa.com e senha 123456"

Esperado:
Bot retorna JSON:
{"plugin":"UserTools","function":"create_user","parameters":{"name":"João","email":"joao@empresa.com","password":"123456"}}

E depois:
"OK: Usuário criado com sucesso."

✅ CORRETO: Chamou a ferramenta quando pedido
```

---

### Teste 5: Solicitação Explícita (Listar) ✅
```
Você escreve:
"Lista todos os usuários"
ou
"Quais usuários existem?"

Esperado:
Bot retorna JSON:
{"plugin":"UserTools","function":"list_users","parameters":{"page":1,"pageSize":10}}

E depois:
Retorna a lista de usuários

✅ CORRETO: Chamou apenas quando pedido explicitamente
```

---

### Teste 6: Solicitação com Filtro ✅
```
Você escreve:
"Lista usuários que contêm 'teste' no nome"

Esperado:
Bot retorna JSON com filtros:
{"plugin":"UserTools","function":"list_users","parameters":{"name":"teste","page":1,"pageSize":10}}

✅ CORRETO: Interpretou a intenção
```

---

### Teste 7: Plugin Novo (TaskPlugin) ✅
```
Você escreve:
"O que você consegue fazer com tarefas?"

Esperado:
Bot descreve as funções do TaskPlugin:
"Posso criar, listar e marcar tarefas como completas..."

Você escreve:
"Cria uma tarefa chamada 'Compilar Projeto'"

Esperado:
Bot invoca TaskTools.create_task

✅ CORRETO: TaskPlugin funcionando automaticamente
```

---

## ✅ Checklist de Sucesso

- [ ] Teste 1: Conversa normal SEM chamar ferramentas
- [ ] Teste 2: Descreve capacidades SEM chamar ferramentas
- [ ] Teste 3: Responde perguntas matemáticas SEM chamar ferramentas
- [ ] Teste 4: Cria usuário QUANDO PEDIDO explicitamente
- [ ] Teste 5: Lista usuários QUANDO PEDIDO explicitamente
- [ ] Teste 6: Entende filtros nas requisições
- [ ] Teste 7: TaskPlugin funciona automaticamente

---

## Problemas Esperados & Soluções

### ❓ Bot ainda chama ferramentas automaticamente?
1. Verifique `FunctionChoiceBehavior.None()` em `ChatIAOrchestrator.cs:103`
2. Verifique que o prompt contém "IMPORTANTES" com instruções
3. Pode ser limitação do modelo Ollama - tente ser mais explícito ao pedir

### ❓ Bot não reconhece quando chamar ferramenta?
1. Use palavras-chave: "criar", "deletar", "listar", "lista"
2. Seja explícito: "Cria um usuário com..."
3. Adicione mais exemplos no prompt se necessário

### ❓ JSON retornado está malformado?
1. Verifique `TryParseToolJson()` em `ChatIAOrchestrator.cs`
2. Ollama pode retornar JSON imperfeito - considere usar parser mais tolerante

### ❓ Novo plugin não aparece no prompt?
1. Verifique se implementa `IPlugin`
2. Verifique se `PluginName` e `Description` estão definidos
3. Rebuild do projeto
4. O método `BuildAvailableToolsDescription()` deve listá-lo automaticamente

---

## Relatório de Teste

Quando testar, anote:

```
Data: __/__/____
Modelo: Ollama llama3.1:latest
Testes passados: ___/7
Observações:
-
-
-
```

---

**Última atualização**: 2026-02-20
**Status**: ✅ Pronto para testes
