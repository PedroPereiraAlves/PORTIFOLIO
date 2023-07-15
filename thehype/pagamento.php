<html>


<head>
<link rel="shortcut icon" href="favicon.png" type="image/x-png"/>
	<meta charset="utf-8">
	<meta http-equiv="X-UA-Compatible" content="IE=edge">
	<meta name="viewport" content="width=device-width, initial-scale=1">
	<meta name="keywords" content="footer, address, phone, icons" />
	
<title> Pagamento - The Hype Co. </title>	
	
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/4.7.0/css/font-awesome.min.css">
   <link rel="stylesheet" href="css/footer-distributed-with-address-and-phones.css">
      <link rel="stylesheet" type="text/css" href="css/pagamento.css">
   <link rel="stylesheet" href="http://maxcdn.bootstrapcdn.com/font-awesome/4.2.0/css/font-awesome.min.css">
   <link href="http://fonts.googleapis.com/css?family=Cookie" rel="stylesheet" type="text/css">
   <script src="http://code.jquery.com/jquery-latest.min.js" type="text/javascript"></script>
   <script src="js/menu.js"></script>
</head>



<body>











<!-- Botão Login --> 


<div id="botão">
<a href="login.html" target="_blank"><font color=gray> <i class="fa fa-lock"></i> Acessar Conta </font></a>
</div>

<div id="line">
</div>






<!-- Logo --> 


<a href="index.html">
<div id="logo">
<img width="200" height="96" src="img/logo.png"/>
</div>
</a>






<!-- Menu --> 


<div id='cssmenu'>
<ul>
   <li><a href='index.html'>Home</a></li>
   <li class='active'><a href='#'>Produtos &nbsp;<i class="fa fa-angle-down"></i> </a>
      <ul>
         <li><a href='#'>Roupas</a>
            <ul>
               <li><a href='#'>Camisetas</a></li>
               <li><a href='#'>Casacos</a></li>
            </ul>
         </li>
         <li><a href='#'>Acessórios</a>
            <ul>
               <li><a href='#'>Relógios</a></li>
               <li><a href='#'>Bonés</a></li>
            </ul>
         </li>
      </ul>
   </li>
   <li><a href='sobre.html'>Sobre Nós</a></li>
   <li><a href='contato.html'>Contato</a></li>
</ul>
</div>

<h1> Realizar Compra </h1>
<div id="line2">
</div>



<!-- Ícones Sociais --> 


<div id="social">
<img width="50" height="50" src="img/twitter.svg" alt="Twitter" /> &nbsp; &nbsp;   
<img width="50" height="50" src="img/instagram.svg" alt="Instagram" />  &nbsp; &nbsp; 
<img width="50" height="50" src="img/facebook.svg" alt="Facebook" /> &nbsp;   &nbsp;
</div>








<div class="row">
  <div class="col-75">
    <div class="container">
      <form method="post" action="C:\xampp\htdocs\projetos 1.7\salvarpagamento.php">
      
        <div class="row">
          <div class="col-50">
            <h3>Informações de Entrega</h3>
            <label for="fname"><i class="fa fa-user"></i> Nome</label>
            <input type="text" id="fname" name="txtnome" placeholder="Insira o nome completo">
            <label for="email"><i class="fa fa-envelope"></i> Email</label>
            <input type="text" id="email" name="txtemail" placeholder="thehypeco@exemplo.com">
            <label for="adr"><i class="fa fa-address-card-o"></i> Endereço</label>
            <input type="text" id="adr" name="txtendereco" placeholder="Rua, Nº">
            <label for="city"><i class="fa fa-institution"></i> Cidade</label>
            <input type="text" id="city" name="txtcidade" placeholder="Ex: Rio de Janeiro">

            <div class="row">
              <div class="col-50">
                <label for="state">Estado</label>
                <input type="text" id="state" name="txtestado" placeholder="Ex: RJ">
              </div>
              <div class="col-50">
                <label for="zip">CEP</label>
                <input type="text" id="zip" name="txtCEP" placeholder="12345-678">
              </div>
            </div>
          </div>

          <div class="col-50">
            <h3>Pagamento</h3>
            <label for="fname">Aceitamos</label>
            <div class="icon-container">
              <i class="fa fa-cc-visa" style="color:navy;"></i>
              <i class="fa fa-cc-amex" style="color:blue;"></i>
              <i class="fa fa-cc-mastercard" style="color:red;"></i>
              <i class="fa fa-cc-discover" style="color:orange;"></i>
            </div>
            <label for="cname">Nome no cartão</label>
            <input type="text" id="cname" name="txtnomecartao" placeholder="Exatamente como no cartão">
            <label for="ccnum">Número do cartão</label>
            <input type="text" id="ccnum" name="txtnumerocartao" placeholder="1111-2222-3333-4444">
            <label for="expmonth">Mês de expiração</label>
            <input type="text" id="expmonth" name="expmonth" placeholder="Ex: Janeiro">
            <div class="row">
              <div class="col-50">
                <label for="expyear">Ano de expiração</label>
                <input type="text" id="expyear" name="expyear" placeholder="Ex: 2020">
              </div>
              <div class="col-50">
                <label for="cvv">CVV</label>
                <input type="text" id="cvv" name="txtcvv" placeholder="352">
              </div>
            </div>
          </div>
          
        </div>
        <label>
          <input type="checkbox" checked="checked" name="sameadr">Li e concordo com os <a href="termos.html">Termos de Serviço</a>.
        </label>
        <input type="submit" value="Concluir Pagamento" class="btn">
      </form>
    </div>
  </div>
</div>


<!-- Rodapé --> 


<footer class="footer-distributed">

			<div class="footer-left">

<div id="logo">
<img width="200" height="96" src="img/logo branco.png"/>
</div>
<br><br><br><br><br>
				<p class="footer-links">
					<a href="index.html">Home</a>
					·
					<a href="sobre.html">Sobre</a>
					·
					<a href="faq.html">Faq</a>
					·
					<a href="contato.html">Contato</a>
				</p>

				<p class="footer-company-name">The Hype Co. &copy; 2019</p>
			</div>

			<div class="footer-center">

				<div>
					<i class="fa fa-map-marker"></i>
					<p><span>Avenida Brasil</span> Rio de Janeiro, Brasil</p>
				</div>

				<div>
					<i class="fa fa-phone"></i>
					<p>+55 (21) 9 1234-5678</p>
				</div>

				<div>
					<i class="fa fa-envelope"></i>
					<p><a href="mailto:thehypeco@mail.com">thehypeco@mail.com</a></p>
				</div>

			</div>

			<div class="footer-right">

				<p class="footer-company-about">
					Empresa focada na comercialização de produtos de alta qualidade visual e material com temática de moda urbana e noturna.
				</p>

				<div class="footer-icons">

					<a href="https://www.facebook.com/thehypeco" target="_blank"><i class="fa fa-facebook"></i></a>
					<a href="https://www.twitter.com/thehypeco" target="_blank"><i class="fa fa-twitter"></i></a>
					<a href="https://www.instagram.com/thehypeco" target="_blank"><i class="fa fa-instagram"></i></a>

				</div>
			</div>
		</footer>


</body>
</html>


