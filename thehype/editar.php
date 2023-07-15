<?php
include("conexao.php");
include("menuproduto.php");
$Codigo = $_GET["Cod_produto"];
settype($Codigo, "integer");

$sql = "select * from produto where Cod_produto = $Codigo";
$query = $mysqli->query($sql);
$dados  = mysqli_fetch_array($query);
$mysqli->close();
?>
<meta charset="utf-8">
 <html>
<head>
<meta>
<title>Cadastro</title>
</head>

<body>
<form id="form1" name="form1" method="post" action="salvar_edicao.php">
<input type="hidden" name="Cod_produto" id="Codigo" value="<?php echo $Codigo;?>" />
  <h2 align="center"><strong>Alteração de Produtos Cadastrados </strong></h2>
  <table width="390" border="1" align="center">
    <tr>
      <td width="165">Nome</td>
      <td width="209"><input name="nome" type="text" id="nome" value="<?php echo $dados["nome"];?>" /></td>
    </tr>
    <tr>
      <td>Preço</td>
      <td><input name="preco" type="text" id="preço" value="<?php echo $dados["preco"];?>" /></td>
    </tr>
    <tr>
      <td>Quantiddade</td>
      <td><input name="quantidade" type="text" id="quantidade" value="<?php echo $dados["quantidade"];?>" /></td>
    </tr>    
    <tr>
      <td>Fornecedor</td>
      <td><input name="fornecedor" type="text" id="fornecedor" value="<?php echo $dados["fornecedor"];?>" /></td>
    </tr>    
  
    <tr>
      <td>&nbsp;</td>
      <td><input type="submit" name="Submit" value="Gravar" style="cursor:pointer"/></td>
    </tr>
  </table>
</form>
</body>
</html>
