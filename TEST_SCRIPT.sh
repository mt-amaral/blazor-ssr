#!/bin/bash
# 🧪 Script de Teste - Fluxo de Chat com Ferramentas

echo "🚀 Iniciando testes do sistema de Chat IA com Ollama + Semantic Kernel"
echo ""

# TESTE 1: Conversa normal
echo "================== TESTE 1: Conversa Normal =================="
echo "Enviando: 'Oi, qual é o seu nome?'"
echo "Esperado: Resposta em texto normal, SEM JSON"
echo "Uma boa resposta seria: 'Oi! Meu nome é um assistente IA. Como posso ajudá-lo?'"
echo ""

# TESTE 2: Solicitação de ferramenta simples
echo "================== TESTE 2: Criar Tarefa =================="
echo "Enviando: 'Cria uma tarefa urgente chamada Comprar Leite'"
echo "Esperado: JSON com call de ferramenta"
echo "Formato esperado:"
echo "{\"plugin\":\"TaskTools\",\"function\":\"create_task\",\"parameters\":{\"title\":\"Comprar Leite\",\"description\":\"\",\"priority\":\"alta\"}}"
echo ""

# TESTE 3: Listar tarefas
echo "================== TESTE 3: Listar Tarefas =================="
echo "Enviando: 'Me mostre todas as tarefas pendentes'"
echo "Esperado: JSON com call para list_tasks"
echo "Formato esperado:"
echo "{\"plugin\":\"TaskTools\",\"function\":\"list_tasks\",\"parameters\":{\"status\":\"pending\"}}"
echo ""

# TESTE 4: Pergunta simples sobre capacidades
echo "================== TESTE 4: Pergunta sobre Capacidades =================="
echo "Enviando: 'O que você consegue fazer?'"
echo "Esperado: Descrição em texto das ferramentas disponíveis, SEM executá-las"
echo ""

# TESTE 5: Instrução contraditória
echo "================== TESTE 5: Instrução Contraditória =================="
echo "Enviando: 'Não preciso de nada, só conversando mesmo'"
echo "Esperado: Resposta em texto normal"
echo ""

# TESTE 6: Ferramenta com parâmetros complexos
echo "================== TESTE 6: Tarefa com Descrição Longa =================="
echo "Enviando: 'Cria uma tarefa de média prioridade chamada Estudar TypeScript com a descrição: Aprender tipos genéricos e decoradores avançados'"
echo "Esperado: JSON com todos os parâmetros preenchidos"
echo ""

echo "💡 DICAS PARA LEITURA DOS LOGS:"
echo ""
echo "Procure por estes padrões nos logs:"
echo ""
echo "✅ SUCESSO - Conversa Normal:"
echo "   🤖 Resposta IA (raw): Oi! Tudo bem..."
echo "   ❌ Nenhum JSON encontrado (debug)"
echo ""
echo "✅ SUCESSO - Tool Call:"
echo "   🤖 Resposta IA (raw): {\"plugin\":\"TaskTools\"...}"
echo "   ✅ Tool call detectado: TaskTools.create_task"
echo "   🔧 Resultado da ferramenta: OK: Tarefa criada com sucesso"
echo ""
echo "❌ PROBLEMA - Tool não retorna JSON:"
echo "   🤖 Resposta IA (raw): Vou criar uma tarefa para você chamada Comprar Leite..."
echo "   ❌ Nenhum JSON encontrado em: Vou criar uma tarefa..."
echo "   → SOLUÇÃO: Melhorar o prompt ou aumentar temperatura do Ollama"
echo ""
echo "❌ PROBLEMA - Formato JSON diferente:"
echo "   🤖 Resposta IA (raw): {\"function\":\"create_task\",\"pluginName\":\"TaskTools\"...}"
echo "   ⚠️ Nenhum campo 'plugin' ou 'name' encontrado"
echo "   → SOLUÇÃO: Adicionar parsing para este formato"
echo ""
