# Destruição de cenários

## Implementação ativa: Voxel Destruction Pro

O fluxo de impacto não usa mais `ChunkedVoxelBuilding`, `VoxelDestructionChunk` ou `IVoxelDamageReceiver`. Objetos destrutíveis devem ser preparados como `DynamicVoxelObj` pelo Voxel Destruction Pro.

Configuração mínima de cada alvo:

- adicione um `VoxelManager` à cena e configure os settings padrão do plugin;
- adicione `DynamicVoxelObj` à raiz do objeto destrutível;
- adicione `NetworkVoxelDestruction` à mesma raiz; o componente também exige um `NetworkObject`;
- use **Quick Setup** no Inspector do componente e atribua ou crie os dados de voxel;
- confirme que `targetFilter`, `targetCollider`, `meshSettings`, `isoSettings` e `dynamicSettings` estão configurados;
- mantenha os colliders atingíveis como filhos do mesmo `DynamicVoxelObj`.

`ProcessHit.VoxelHit` recebe ponto e normal em coordenadas mundiais, mas o raio é expresso diretamente em voxels. Assim, `destructionRadius = 10` limita a remoção a 10 células do centro do impacto. Para destruição em linha, o comprimento mundial é calculado como `raioEmVoxels * GetSingleVoxelSize()`.

O tipo `Sphere` usa uma borda orgânica determinística em vez de uma esfera perfeita. No `DynSettings`, `sphereSurfaceIrregularity` controla a profundidade proporcional das reentrâncias (`0` restaura a esfera perfeita) e `sphereNoiseFrequency` controla quantas irregularidades aparecem na superfície. O raio configurado permanece como limite máximo: com intensidade `0.18` e raio `10`, a borda varia aproximadamente entre `8.2` e `10` voxels. O servidor calcula os índices exatos e os replica, portanto todos os clientes recebem a mesma forma.

`Projectile` envia o pedido ao `NetworkVoxelDestruction`. O servidor valida, ordena e executa o job destrutor; somente depois do cálculo ele compacta os índices exatos removidos em intervalos e os adiciona à `SyncList`. Os clientes aplicam esses índices sem executar novamente o job geométrico e usam a seed definida pelo servidor para reconstruir os mesmos fragmentos. O histórico também é aplicado quando um cliente entra depois. `DummyProjectile` executa somente efeitos e nunca solicita destruição.

Chamadas diretas do próprio plugin, como `AddDestruction`, `AddDestruction_Sphere`, `VoxCollider` e os scripts de demo, também são encaminhadas para `NetworkVoxelDestruction` enquanto FishNet estiver ativo. Se o alvo não possuir o componente de rede, a destruição é bloqueada para evitar divergência entre clientes.

Use `MeshSettingsObj.EmptyAction.None` nos objetos de rede. As opções `Destroy` e `Deactive` removem ou desativam localmente a raiz que contém o `NetworkObject`, interferindo na replicação e no histórico persistente.

## Três modos legados para objetos pequenos

| Modo | Componente | Configuração |
| --- | --- | --- |
| Peça inteira cai | `VoxelPartialCollapse` | Mesh intacta + collider de destroço. O Rigidbody fica cinemático até a destruição. |
| Peça se fragmenta | `VoxelFragmentDestruction` no objeto intacto; `VoxelFragmentedObj` em cada filho pré-fraturado | Cada fragmento possui mesh, Rigidbody, collider e NetworkTransform próprios. Os GameObjects e seus pais devem permanecer ativos; o script controla a visibilidade. |
| Destruição por eventos | `VoxelFullCollapse` no controlador da construção | `collapseTriggers` recebe as peças que disparam o evento; `collapseTargets` recebe as peças a destruir. Qualquer `VoxelDestruction` pode ser gatilho. |

Não há corte de mesh em runtime: as partes do modo 2 devem ser criadas no Blender/editor. Use poucas partes físicas significativas por peça. O Rigidbody é preparado previamente, evitando AddComponent no momento do impacto.

No modo 3, `requiredDestroyedTriggers = 0` exige todos os gatilhos válidos; um valor positivo permite, por exemplo, derrubar a construção após dois pilares. Listas vazias descobrem os componentes filhos automaticamente: os gatilhos são `VoxelFullCollapseTrigger`, e os alvos são as peças destrutíveis, excluindo os fragmentos (ativados pelo próprio pai). Referências duplicadas de gatilhos são ignoradas. Não use esta descoberta para agrupar várias construções independentes sob um mesmo controlador.

## Preparar cenas e prefabs existentes

1. Espere o Unity recompilar e selecione o GameObject da peça na Hierarchy ou no Prefab Mode. Ele deve conter MeshFilter com mesh e MeshRenderer. É possível selecionar várias peças sem sobrepor pais e filhos.
2. Execute **Tools > Warxel > Destruction > Prepare selected hierarchy** para abrir a janela. Escolha **Peça inteira**, **Fragmentação** ou **Por evento**, informe o dano e clique em **Aplicar aos selecionados**. O comando adiciona os componentes necessários mesmo em objetos sem scripts de destruição. A operação admite Undo e não salva automaticamente.
3. Revise os BoxColliders gerados. A caixa cobre os bounds locais da mesh e pode ser inadequada para arcos, formas côncavas ou peças muito finas. Ajuste-a ou atribua um collider convexo pré-construído em `debrisCollider`. O collider intacto deve ser um MeshCollider separado, não convexo.
4. Mantenha todos os pais dos fragmentos ativos e use raízes estáveis. Não anime o transform que contém corpos sincronizados; aplique animações decorativas numa hierarquia visual separada.
5. Salve a cena/prefab. Revise as condições de observação do FishNet: todos os jogadores que precisam ver a construção devem ser observers dela e de suas peças. Prefabs instanciados em runtime devem estar registrados e ser spawned pelo servidor.

O comando configura autoridade do servidor, envio ao owner, posição/rotação, intervalo de três ticks, Rigidbody cinemático, solver com quatro iterações e repouso habilitado. Remove flags Static das peças e prepara colliders de caixa quando não existe um collider de destroço válido atribuído. As alterações não foram aplicadas automaticamente às cenas/prefabs nesta implementação.

Na opção **Fragmentação**, o selecionado deve ser a mesh intacta e seus descendentes com MeshFilter devem conter as meshes pré-fraturadas. A janela configura esses descendentes como `VoxelFragmentedObj` e oculta seus renderers/colliders intactos. Não coloque um fragmento dentro de outro; use filhos irmãos ou grupos sem mesh. O comando não gera cortes na mesh.

Na opção **Por evento**, o selecionado recebe `VoxelFullCollapseTrigger`. Adicione os GameObjects que também devem ser destruídos à lista de alvos. Eles são ligados pelo campo `chainCollapse`, que aceita tanto peças inteiras quanto raízes de fragmentação. Alvos sem destruição recebem `VoxelPartialCollapse`; os já configurados mantêm seu modo. O gatilho e os alvos devem estar na mesma cena/prefab, em ramos separados. Esse fluxo dispara ao destruir qualquer gatilho selecionado; para exigir vários pilares destruídos em conjunto, use o controlador `VoxelFullCollapse` descrito acima.

Aplicar novamente o mesmo modo reutiliza seus componentes e colliders. Trocar o modo substitui o componente de destruição, preservando material e configurações comuns compatíveis; revise referências externas ao componente antigo. O dano e as ligações de evento são definidos pelos valores atuais da janela. Para peças inteiras, a janela limpa `chainCollapse`. Ctrl+Z desfaz a operação inteira, incluindo a configuração dos alvos.

## Rede e física

O servidor decide a destruição, simula Rigidbody e executa cascatas. Os clientes mantêm os corpos cinemáticos e recebem movimento via NetworkTransform. Os estados intacto/destruído/repouso usam SyncVars, incluindo clientes que entram depois. Não desative o NetworkTransform quando o destroço parar: ele ainda deve atender novos observers e resets; o FishNet envia propriedades quando mudam.

Para garantir autoridade, desative **Client Authoritative** no NetworkTransform e mantenha **Send To Owner** habilitado. **Component Configuration** deve ser Disabled: o componente de destruição controla `isKinematic`. O script também desabilita essa configuração automática ao inicializar.

O fluxo de dano existente (`TakeDamage` / `ApplyDamageOnServer`) foi preservado. O RPC existente ainda aceita pedidos de dano dos clientes; isso não implementa validação autoritativa de acerto de armas.

A animação usa o parâmetro `FullCollapse`. Para quem entra depois, configure `collapsedAnimatorState` com o nome/caminho do estado final, por exemplo `Base Layer.Collapsed`; ele será aplicado no final da animação. Sem esse campo, um cliente tardio reproduz a animação desde o início, embora as peças físicas já recebam o estado atual. `ResetFullCollapse` restaura o estado inicial do Animator capturado na inicialização.

## Controles de custo

- Em `SphereBasedFragments`, `adaptSphereFragmentsToDestructionRadius` faz uma interpolação pelo raio em voxels. `smallDestructionVoxelRadius` usa `smallImpactFragmentRadius` e `smallImpactFragmentCount`; `largeDestructionVoxelRadius` usa os equivalentes `largeImpact...`. Entre os extremos, tamanho e quantidade variam continuamente: o tamanho aumenta enquanto a quantidade diminui. O servidor replica o mesmo raio e seed, portanto todos calculam os mesmos parâmetros adaptativos.
- Na fragmentação adaptativa, todos os voxels removidos são divididos por cortes espaciais determinísticos em regiões com caixas delimitadoras não sobrepostas. A quantidade-alvo é limitada antes do job por `maxFragmentsPerDestruction`, e nenhum grupo excedente é descartado depois. Portanto, toda a matéria removida pertence a um fragmento sem multiplicar as grandes matrizes densas usadas para construir suas malhas.
- O `SphereFragmenterJob` mantém um mapa nativo de índice para posição. Consultar e retirar um voxel fragmentado é uma operação de tempo constante; não existe mais uma varredura completa dos voxels removidos para cada célula da esfera. Isso é especialmente importante para explosões de raio grande.
- Os fragmentos do `DynamicVoxelObj` usam um orçamento centralizado em `VoxelFragmentBudget`. O asset `DynSettings` controla `maxFragmentsPerDestruction`, `maxActiveFragments`, `fragmentLifetime`, o tempo mínimo de física e o repouso antes de congelar. O limite global remove primeiro os fragmentos mais antigos.
- Lotes de fragmentos de vários acertos são materializados em round-robin. Um lote ainda sendo exibido não bloqueia o job do próximo tiro; `maxFragmentsPerFrame` é um orçamento total compartilhado entre os acertos pendentes naquele objeto, evitando tanto a espera serial quanto a multiplicação do pico por arma automática.
- Depois que a malha e o collider de um fragmento ficam prontos, `releaseFragmentVoxelData` libera os arrays nativos e jobs usados para construí-lo. O fragmento continua visível, mas passa a ser apenas destroço visual e não aceita nova destruição voxel. `disableFragmentCollisionAfterSleep` e `disableFragmentShadows` reduzem, respectivamente, o custo de PhysX e renderização.
- `maxFragmentsPerFrame` apenas distribui a criação entre frames. Ele reduz o pico de um único frame, mas não controla sozinho a quantidade total; use-o junto dos dois limites de fragmentos.
- `VoxelDestructionScheduler` processa até **16 solicitações de cascata/fragmento por passo de física**, globalmente. Pode adicionar esse componente à cena para ajustar `activationsPerPhysicsStep`; caso contrário ele é criado automaticamente. Acertos diretos continuam imediatos. A fila evita recursão e distribui as ativações, mas não limita o total de corpos já em movimento.
- `settleWhenResting` torna destroços cinemáticos depois de dormirem ou permanecerem lentos com apoio estacionário pelo período configurado. Eles permanecem sólidos e não voltam a simular até reset. Desative a opção para destroços que precisam ser empurrados ou reagir à retirada posterior de seu apoio.
- Colliders pré-construídos evitam gerar convex meshes na inicialização. Sem `debrisCollider`, existe fallback para convex mesh, que continua tendo custo de cooking.
- A frequência de movimento é `tick rate / interval`: três ticks num servidor de 30 Hz correspondem a até 10 atualizações/s por peça em movimento. Ajuste conforme latência e qualidade visual.
- `debrisLayer` permite usar a matriz de colisão do projeto para reduzir contatos entre destroços. O comando não modifica layers nem a matriz global. Se usar `VoxelDebris`, revise colisões com cenário, jogadores e veículos.

Os três modos legados mantêm um objeto de rede por peça física e devem ficar restritos a objetos pequenos. Para edifícios grandes, use `DynamicVoxelObj` e controle o custo pelos settings de meshing, isolamento e fragmentação do Voxel Destruction Pro.

## Reset e validação em Play Mode

No servidor, use `ResetCollapse`, `ResetFragments` ou `ResetFullCollapse`. O reset invalida solicitações pendentes para que peças não sejam destruídas novamente por uma cascata anterior. O reset completo também restaura os alvos, não apenas os gatilhos.

Validar com servidor dedicado + dois clientes, e repetir com host:

1. Danificar uma peça do modo 1: apenas o Rigidbody do servidor fica dinâmico; clientes acompanham posição e rotação.
2. Destruir uma peça do modo 2: objeto intacto desaparece, fragmentos aparecem nos clientes; novos impactos no pai não repetem a destruição.
3. Atingir um de dois pilares: nenhuma cascata. Atingir o segundo: disparar uma única cascata. Testar referências cíclicas em `chainCollapse` e alvos repetidos.
4. Conectar outro cliente durante o movimento e novamente após repouso: confirmar meshes, colliders, transformações e estado final do Animator.
5. Executar reset durante uma cascata grande e após repouso: confirmar posições originais, visibilidade, modelos de dano, gatilhos e ausência de ativações antigas. Destruir novamente.
6. No Profiler, comparar a mesma construção antes/depois: tempo de Physics.Simulate, corpos ativos, contatos, GC.Alloc e tráfego enviado pelo servidor. Repetir com o número máximo esperado de construções simultâneas.

Validação realizada nesta alteração: compilação C# dos scripts de runtime e Editor com o compilador e assemblies locais do Unity/FishNet; `git diff --check`. A geração de código de rede do FishNet, testes multiplayer em Play Mode e medições de performance ainda precisam ser executados no Unity.

Referência de configuração: https://fish-networking.gitbook.io/docs/fishnet-building-blocks/components/network-transform
