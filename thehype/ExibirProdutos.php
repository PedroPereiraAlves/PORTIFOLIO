<?php

// INCLUI A LISTA DE PRODUTOS CADASTRADOS

include "menuproduto.php";

?>

<html>

<head>

<meta http-equiv="Content-Type" content="text/html; charset=UTF-8">

<title>Controle de Produto</title>

</head>

<body>

<?php

// INCLUIR O ARQUIVO DE CONEXÃO COM O BANCO DE DADOS

include "conexao.php";

// VARIÁVEL COM O COMANDO DE CONSULTA NA TABELA PRODUTO
$query = "select Cod_produto, Fornecedor, nome, preco, Quantidade";

// COMANDO PARA EXECUTAR A CONSULTA NO BANCO

$result = mysql_query($query) or die(mysql_error());

?>

<h2> Lista de Produtos </h2>

<!-- CRIA UMA TABELA NO HTML -->

<table border="0" width="70%" align="center">

<tr bgcolor="lightblue">

<th>Nome</th>

<th>Preço</th>

<th>Fornecedor</th>

<th>Quantidade</th>


</tr>

<?php

// A FUNÇÃO mysql_fetch_array TRANSFORMA O RESULTADO DA QUERY EM UM ARRAY

// A ESTRUTURA WHILE EXIBE TODOS OS ELEMENTOS DO ARRAY $row.

while($row = mysql_fetch_array($result))

{

// RECEBE O ID DO PRODUTO NO BANCO

$id = $row['id'];
// ESCREVE UMA LINHA NA TABELA

echo "<tr>";

// ESCREVE UMA COLUNA NA TABELA E EXIBE O NOME DO PRODUTO

echo "<td>" . $row['nome'] . "</td>";

echo "<td>" . $row['Preco'] . "</td>";

echo "<td>" . $row['Fornecedor'] . "</td>";

echo "<td>" . $row['Quantidade'] . "</td>";

// CRIAR UM LINK PARA EXCLUIR OU ALTERAR PASSANDO VIA GET O ID

echo "<td><a href='formalterarproduto.php?txtid=$id'> Alterar </a> || <a href='formexcluirproduto.php?txtid=$id'> Excluir</a></td>";

echo "</tr>";

}

?>

</table>

<br><br>

<a href="formcadastrarproduto.php">Inserir um Novo Produto</a>

</body>

</html>